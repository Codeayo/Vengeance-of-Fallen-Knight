using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>Rebuilds the knight's and enemies' Animator Controllers with complete, responsive state machines.</summary>
public static class BuildAnimators
{
    const string OutDir = "Assets/Animations/Generated";
    static StringBuilder log;

    static Sprite[] Frames(string png) =>
        AssetDatabase.LoadAllAssetsAtPath(png).OfType<Sprite>()
            .OrderBy(s => { var p = s.name.Split('_'); return int.TryParse(p.Last(), out int n) ? n : 0; }).ToArray();

    /// <summary>Creates (or replaces) a sprite clip from frames.</summary>
    static AnimationClip Clip(string name, Sprite[] frames, float fps, bool loop, string target = "")
    {
        UIKit.EnsureDir(OutDir);
        string path = $"{OutDir}/{name}.anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, path); }
        clip.ClearCurves();
        clip.frameRate = fps;
        var binding = new EditorCurveBinding { type = typeof(SpriteRenderer), path = target, propertyName = "m_Sprite" };
        var keys = new ObjectReferenceKeyframe[frames.Length + 1];
        for (int i = 0; i < frames.Length; i++) keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = frames[i] };
        keys[frames.Length] = new ObjectReferenceKeyframe { time = frames.Length / fps, value = frames[frames.Length - 1] };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    static void SetLoop(AnimationClip c, bool loop)
    {
        var s = AnimationUtility.GetAnimationClipSettings(c);
        s.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(c, s);
        EditorUtility.SetDirty(c);
    }

    static AnimationClip Existing(string path) => AssetDatabase.LoadAssetAtPath<AnimationClip>(path);

    // ---------------- controller helpers ----------------
    static AnimatorController Reset(string path, params (string name, AnimatorControllerParameterType type)[] ps)
    {
        var c = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (c == null) c = AnimatorController.CreateAnimatorControllerAtPath(path);
        foreach (var p in c.parameters.ToArray()) c.RemoveParameter(p);
        foreach (var (n, t) in ps) c.AddParameter(n, t);
        var sm = c.layers[0].stateMachine;
        foreach (var s in sm.states.ToArray()) sm.RemoveState(s.state);
        foreach (var t in sm.anyStateTransitions.ToArray()) sm.RemoveAnyStateTransition(t);
        foreach (var t in sm.entryTransitions.ToArray()) sm.RemoveEntryTransition(t);
        return c;
    }

    static AnimatorState State(AnimatorController c, string name, Motion m, float speed = 1f)
    {
        var s = c.layers[0].stateMachine.AddState(name);
        s.motion = m;
        s.speed = speed;
        s.writeDefaultValues = false;
        return s;
    }

    static AnimatorStateTransition T(AnimatorState from, AnimatorState to, float exitTime = -1f)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = exitTime >= 0f;
        if (exitTime >= 0f) t.exitTime = exitTime;
        t.duration = 0f;
        t.hasFixedDuration = true;
        return t;
    }

    static AnimatorStateTransition Any(AnimatorController c, AnimatorState to, bool canSelf = false)
    {
        var t = c.layers[0].stateMachine.AddAnyStateTransition(to);
        t.hasExitTime = false;
        t.duration = 0f;
        t.hasFixedDuration = true;
        t.canTransitionToSelf = canSelf;
        return t;
    }

    const AnimatorConditionMode If = AnimatorConditionMode.If, IfNot = AnimatorConditionMode.IfNot,
        Gt = AnimatorConditionMode.Greater, Lt = AnimatorConditionMode.Less, Eq = AnimatorConditionMode.Equals;
    const AnimatorControllerParameterType B = AnimatorControllerParameterType.Bool, Tr = AnimatorControllerParameterType.Trigger,
        F = AnimatorControllerParameterType.Float, I = AnimatorControllerParameterType.Int;

    // ---------------- knight ----------------
    static void Knight()
    {
        string dir = "Assets/Players/SoulslikeKnight/SoulslikeKnight_SpriteSheets/";
        var idle = Existing(dir + "Idle.anim"); SetLoop(idle, true);
        var run = Existing(dir + "Run.anim"); SetLoop(run, true);
        var a1 = Existing(dir + "Attack1.anim"); SetLoop(a1, false);
        var a2 = Existing(dir + "Attack2.anim"); SetLoop(a2, false);
        var a3 = Existing(dir + "Attack3.anim"); SetLoop(a3, false);
        var block = Existing(dir + "Block.anim"); SetLoop(block, true);
        var heal = Existing(dir + "Heal.anim"); SetLoop(heal, false);
        var hurt = Existing(dir + "Hurt.anim"); SetLoop(hurt, false);
        var roll = Existing(dir + "Roll.anim"); SetLoop(roll, false);
        var jumpFrames = Frames(dir + "SoulslikeKnight_Jump.png");
        var rise = Clip("Knight_JumpRise", jumpFrames.Take(2).ToArray(), 8, false);
        var fall = Clip("Knight_Fall", new[] { jumpFrames.Last() }, 8, true);
        var deathFrames = Frames(dir + "SoulslikeKnight_Death.png");
        var death = Clip("Knight_Death", deathFrames, 9, false);
        var resurrect = Clip("Knight_Resurrect", deathFrames.Reverse().ToArray(), 6, false);

        var c = Reset(dir + "Player.controller",
            ("IsRunning", B), ("IsGrounded", B), ("YVel", F), ("IsBlocking", B), ("IsHealing", B), ("IsDead", B),
            ("Attack", Tr), ("CombpStep", I), ("Roll", Tr), ("Jump", Tr), ("Hurt", Tr), ("Death", Tr), ("Rise", Tr));
        var sm = c.layers[0].stateMachine;
        var sIdle = State(c, "Idle", idle); sm.defaultState = sIdle;
        var sRun = State(c, "Run", run);
        var sJump = State(c, "Jump", rise);
        var sFall = State(c, "Fall", fall);
        var sA1 = State(c, "Attack1", a1, 1.2f);
        var sA2 = State(c, "Attack2", a2, 1.2f);
        var sA3 = State(c, "Attack3", a3, 1.1f);
        var sBlock = State(c, "Block", block);
        var sHeal = State(c, "Heal", heal);
        var sHurt = State(c, "Hurt", hurt, 1.3f);
        var sRoll = State(c, "Roll", roll, 1.15f);
        var sDeath = State(c, "Death", death);
        var sRise = State(c, "Resurrect", resurrect);

        // Highest priority first.
        Any(c, sDeath).AddCondition(If, 0, "IsDead");
        var r = Any(c, sRise); r.AddCondition(If, 0, "Rise");
        var h = Any(c, sHurt); h.AddCondition(If, 0, "Hurt"); h.AddCondition(IfNot, 0, "IsDead");
        var ro = Any(c, sRoll); ro.AddCondition(If, 0, "Roll"); ro.AddCondition(IfNot, 0, "IsDead");
        foreach (var (st, step) in new[] { (sA1, 0), (sA2, 1), (sA3, 2) })
        {
            var t = Any(c, st, true); t.AddCondition(If, 0, "Attack"); t.AddCondition(Eq, step, "CombpStep"); t.AddCondition(IfNot, 0, "IsDead");
        }
        var j = Any(c, sJump); j.AddCondition(If, 0, "Jump"); j.AddCondition(IfNot, 0, "IsDead");
        var bl = Any(c, sBlock); bl.AddCondition(If, 0, "IsBlocking"); bl.AddCondition(If, 0, "IsGrounded"); bl.AddCondition(IfNot, 0, "IsDead");
        var he = Any(c, sHeal); he.AddCondition(If, 0, "IsHealing"); he.AddCondition(IfNot, 0, "IsDead");

        T(sIdle, sRun).AddCondition(If, 0, "IsRunning");
        T(sRun, sIdle).AddCondition(IfNot, 0, "IsRunning");
        foreach (var s in new[] { sIdle, sRun }) { var f = T(s, sFall); f.AddCondition(IfNot, 0, "IsGrounded"); f.AddCondition(Lt, -1.5f, "YVel"); }
        T(sJump, sFall).AddCondition(Lt, 0f, "YVel");
        foreach (var s in new[] { sJump, sFall })
        {
            var l1 = T(s, sRun); l1.AddCondition(If, 0, "IsGrounded"); l1.AddCondition(If, 0, "IsRunning");
            var l2 = T(s, sIdle); l2.AddCondition(If, 0, "IsGrounded"); l2.AddCondition(IfNot, 0, "IsRunning");
        }
        foreach (var s in new[] { sA1, sA2, sA3, sHurt, sRoll, sRise, sHeal }) T(s, sIdle, 0.95f);
        T(sBlock, sIdle).AddCondition(IfNot, 0, "IsBlocking");
        EditorUtility.SetDirty(c);
        log.AppendLine("knight: 13 states (idle, run, jump, fall, 3-hit combo, block, heal, hurt, roll, death, resurrect)");
    }

    // ---------------- shield skeleton ----------------
    static void Skeleton()
    {
        string dir = "Assets/Enemy/SKeleton/Skeleton_Warrior/";
        var idle = Clip("Skel_Idle", Frames(dir + "Idle.png"), 8, true);
        var walk = Clip("Skel_Walk", Frames(dir + "Walk.png"), 9, true);
        var run = Clip("Skel_Run", Frames(dir + "Run.png"), 12, true);
        var a1 = Clip("Skel_Attack1", Frames(dir + "Attack_1.png"), 11, false);
        var a2 = Clip("Skel_Attack2", Frames(dir + "Attack_2.png"), 12, false);
        var a3 = Clip("Skel_Attack3", Frames(dir + "Attack_3.png"), 11, false);
        var runAtk = Clip("Skel_RunAttack", Frames(dir + "Run+attack.png"), 12, false);
        var hurt = Clip("Skel_Hurt", Frames(dir + "Hurt.png"), 8, false);
        var dead = Clip("Skel_Death", Frames(dir + "Dead.png"), 7, false);
        var protect = Clip("Skel_Protect", Frames(dir + "Protect.png"), 4, true);

        var c = Reset("Assets/Animations/PatrolSkeleton.controller",
            ("Speed", F), ("Attack", Tr), ("AttackIndex", I), ("Hurt", Tr), ("Dead", B), ("Protect", B),
            ("PlayerDetected", B), ("PlayerInRange", B));
        var sm = c.layers[0].stateMachine;
        var sIdle = State(c, "Idle", idle); sm.defaultState = sIdle;
        var sWalk = State(c, "Walk", walk);
        var sRun = State(c, "Run", run);
        var sDeath = State(c, "Death", dead);
        var sHurt = State(c, "Hurt", hurt);
        var sProt = State(c, "Protect", protect);
        Any(c, sDeath).AddCondition(If, 0, "Dead");
        var h = Any(c, sHurt); h.AddCondition(If, 0, "Hurt"); h.AddCondition(IfNot, 0, "Dead");
        int i = 0;
        foreach (var m in new Motion[] { a1, a2, a3, runAtk })
        {
            var s = State(c, "Attack" + (i + 1), m);
            var t = Any(c, s); t.AddCondition(If, 0, "Attack"); t.AddCondition(Eq, i, "AttackIndex"); t.AddCondition(IfNot, 0, "Dead");
            T(s, sIdle, 0.96f);
            i++;
        }
        var p = Any(c, sProt); p.AddCondition(If, 0, "Protect"); p.AddCondition(IfNot, 0, "Dead");
        T(sProt, sIdle).AddCondition(IfNot, 0, "Protect");
        T(sHurt, sIdle, 0.95f);
        T(sIdle, sWalk).AddCondition(Gt, 0.1f, "Speed");
        var wr = T(sWalk, sRun); wr.AddCondition(Gt, 1.9f, "Speed");
        T(sWalk, sIdle).AddCondition(Lt, 0.1f, "Speed");
        T(sRun, sWalk).AddCondition(Lt, 1.9f, "Speed");
        T(sIdle, sRun).AddCondition(Gt, 1.9f, "Speed");
        EditorUtility.SetDirty(c);
        log.AppendLine("skeleton: idle, walk, run, 3 attacks + run-attack, hurt, protect, death");
    }

    // ---------------- axe skeleton ----------------
    static void Axe()
    {
        string dir = "Assets/Enemy/Skeleton 1/Sprite Sheets/";
        var idle = Clip("Axe_Idle", Frames(dir + "Skeleton Idle.png"), 10, true, "enemy_spirtes");
        var walk = Clip("Axe_Walk", Frames(dir + "Skeleton Walk.png"), 13, true, "enemy_spirtes");
        var atk = Clip("Axe_Attack", Frames(dir + "Skeleton Attack.png"), 16, true, "enemy_spirtes");
        var hit = Clip("Axe_Hit", Frames(dir + "Skeleton Hit.png"), 14, false, "enemy_spirtes");
        var dead = Clip("Axe_Death", Frames(dir + "Skeleton Dead.png"), 13, false, "enemy_spirtes");
        var react = Clip("Axe_React", Frames(dir + "Skeleton React.png"), 10, false, "enemy_spirtes");

        var c = Reset("Assets/Animations/EnemyAnimation/Enemy.controller",
            ("canWalk", B), ("Attack", B), ("Hurt", Tr), ("Death", Tr), ("Alert", Tr));
        var sm = c.layers[0].stateMachine;
        var sIdle = State(c, "Idle", idle); sm.defaultState = sIdle;
        var sWalk = State(c, "Walk", walk);
        var sAtk = State(c, "Attack", atk);
        var sHit = State(c, "Hit", hit);
        var sDead = State(c, "Death", dead);
        var sReact = State(c, "Alert", react);
        Any(c, sDead).AddCondition(If, 0, "Death");
        Any(c, sHit).AddCondition(If, 0, "Hurt");
        Any(c, sReact).AddCondition(If, 0, "Alert");
        T(sIdle, sWalk).AddCondition(If, 0, "canWalk");
        T(sWalk, sIdle).AddCondition(IfNot, 0, "canWalk");
        T(sIdle, sAtk).AddCondition(If, 0, "Attack");
        T(sWalk, sAtk).AddCondition(If, 0, "Attack");
        T(sAtk, sIdle).AddCondition(IfNot, 0, "Attack");
        T(sHit, sIdle, 0.95f);
        T(sReact, sIdle, 0.95f);
        EditorUtility.SetDirty(c);
        log.AppendLine("axe skeleton: idle, walk, attack, hit, alert, death (from the unused sprite sheets)");
    }

    // ---------------- ninja ----------------
    static void Ninja()
    {
        string dir = "Assets/Enemy/YellowNinja/";
        var idle = Clip("Ninja_Idle", Frames(dir + "yellowNinja - idle.png"), 10, true);
        var walk = Existing(dir + "YellowNinjaWalk.anim"); SetLoop(walk, true);
        var atk = Existing(dir + "YellowNinjaAttack.anim"); SetLoop(atk, true);
        var hit = Existing(dir + "YellowNinjaHit.anim"); SetLoop(hit, false);
        var death = Existing(dir + "YellowNinjaDeath.anim"); SetLoop(death, false);

        var c = Reset(dir + "Enemy_1.controller", ("Attack", B), ("Hurt", Tr), ("Death", Tr), ("Speed", F));
        var sm = c.layers[0].stateMachine;
        var sIdle = State(c, "Idle", idle); sm.defaultState = sIdle;
        var sWalk = State(c, "Walk", walk);
        var sAtk = State(c, "Attack", atk, 1.25f);
        var sHit = State(c, "Hit", hit);
        var sDeath = State(c, "Death", death);
        Any(c, sDeath).AddCondition(If, 0, "Death");
        Any(c, sHit).AddCondition(If, 0, "Hurt");
        T(sIdle, sWalk).AddCondition(Gt, 0.1f, "Speed");
        T(sWalk, sIdle).AddCondition(Lt, 0.1f, "Speed");
        T(sIdle, sAtk).AddCondition(If, 0, "Attack");
        T(sWalk, sAtk).AddCondition(If, 0, "Attack");
        T(sAtk, sIdle).AddCondition(IfNot, 0, "Attack");
        var hw = T(sHit, sIdle, 0.95f);
        EditorUtility.SetDirty(c);
        log.AppendLine("ninja: idle (new), walk, attack, hit, death");
    }

    public static string Build()
    {
        log = new StringBuilder();
        Knight(); Skeleton(); Axe(); Ninja();
        AssetDatabase.SaveAssets();
        return log.ToString();
    }
}
