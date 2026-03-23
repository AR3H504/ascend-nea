using UnityEngine;

// A checkpoint variant for the city section that uses
// a faint green when inactive and the normal green when active.
public class CityCheckpoint : Checkpoint
{
    protected override void Awake()
    {
        inactiveColor = new Color(0.65f, 0.9f, 0.7f, 1f);
        activeColor = Color.green;
    }
}
