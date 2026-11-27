using UnityEngine;
public static class ShowRead {
  public static string Run(){
    var inv=Object.FindFirstObjectByType<InventoryUI>(); if(InventoryUI.IsOpen) inv.Close();
    ReadPanel.Show("Hermit's Warning","Turn back, traveller. Since the usurper's mages came, the dead do not stay in the earth here.\n\nIf you must pass, keep to the high caps of the great mushrooms. The rot sleeps below.");
    return "shown";
  }
}
