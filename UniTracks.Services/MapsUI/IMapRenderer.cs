using System.Drawing;

namespace UniTracks.Services.MapsUI;

public interface IMapRenderer
{
    /// <summary>
    /// Renders an image of the map.
    /// </summary>
    /// <returns>The rendered image as a byte array.</returns>
    byte[] RednderImage();

    /// <summary>
    /// Renders an image of the map centered at the specified point with the given width, height, resolution, and rotation.
    /// </summary>
    /// <param name="point">The center point of the image.</param>
    /// <param name="width">The width of the image.</param>
    /// <param name="height">The height of the image.</param>
    /// <param name="resolution">The resolution of the image.</param>
    /// <param name="rotation">The rotation angle of the image.</param>
    /// <returns>The rendered image as a byte array.</returns>
    byte[] RednderImage(System.Drawing.Point point, double width, double height, double resolution = 1, double rotation = 0);

    /// <summary>
    /// Renders an image of the map within the specified rectangle with the given resolution and rotation.
    /// </summary>
    /// <param name="rectangle">The rectangle defining the area to render.</param>
    /// <param name="resolution">The resolution of the image.</param>
    /// <param name="rotation">The rotation angle of the image.</param>
    /// <returns>The rendered image as a byte array.</returns>
    byte[] RednderImage(Rectangle rectangle, double resolution = 1, double rotation = 0);
}
