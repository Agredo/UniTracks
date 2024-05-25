using Mapsui;
using Mapsui.Rendering;
using Mapsui.Rendering.Skia;
using Mapsui.UI.Maui;
using System.Drawing;
using UniTracks.Services.MapsUI;

namespace UniTracks.Maui.Services.Mapsui;


public class MapsUIRenderer : IMapRenderer
{
    public MapControl MapControl { get; private set; }
    public MapRenderer Renderer { get; private set; }

    public void Init()
    {
        MapControl = new MapControl();
        Renderer = new MapRenderer();
    }

    public byte[] RednderImage(System.Drawing.Point point, double width, double height, double resolution = 1, double rotation = 0)
    {
        var centerX = point.X;
        var centerY = point.Y;

        Viewport viewport = new Viewport(centerX, centerY, resolution, rotation, width, height);

        return Renderer.RenderToBitmapStream(viewport, MapControl.Map.Layers).ToArray();

    }

    public byte[] RednderImage(Rectangle rectangle, double resolution = 1, double rotation = 0)
    {
        var centerX = rectangle.X + rectangle.Width / 2;    
        var centerY = rectangle.Y + rectangle.Height / 2;

        var width = rectangle.Width;
        var height = rectangle.Height;


        Viewport viewport = new Viewport(centerX, centerY, resolution, rotation, width, height);

        return Renderer.RenderToBitmapStream(viewport, MapControl.Map.Layers).ToArray();

    }

    public byte[] RednderImage(double centerX, double centerY, double width, double height, double resolution = 1, double rotation = 0)
    {
        Viewport viewport = new Viewport(centerX, centerY, resolution, rotation, width, height);

        return Renderer.RenderToBitmapStream(viewport, MapControl.Map.Layers).ToArray();

    }

    public byte[] RednderImage()
    {
        IRenderer renderer = new MapRenderer();
        return renderer.RenderToBitmapStream(ToViewport(MapControl.Map.Extent!.Multiply(3), 256), MapControl.Map.Layers).ToArray();

    }

    public static Viewport ToViewport(MRect rect, double width)
    {
        return new Viewport(
            rect.Centroid.X,
            rect.Centroid.Y,
            rect.Width / width,
            0,
            width,
            width * (rect.Height / rect.Width)
        );
    }
}

