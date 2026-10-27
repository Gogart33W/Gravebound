using Godot;

namespace Gravebound.World;

public partial class TestArena : Node3D
{
    public override void _Ready()
    {
        var navRegion = GetNodeOrNull<NavigationRegion3D>("NavigationRegion3D");
        if (navRegion != null)
        {
            if (navRegion.NavigationMesh == null)
            {
                navRegion.NavigationMesh = new NavigationMesh
                {
                    GeometryParsedGeometryType = NavigationMesh.ParsedGeometryType.StaticColliders
                };
            }
            navRegion.BakeNavigationMesh(onThread: false);
        }
    }
}
