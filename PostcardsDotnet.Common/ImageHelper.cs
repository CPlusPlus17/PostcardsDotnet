using ImageMagick;
using PostcardDotnet.Contracts;

namespace PostcardDotnet.Common;

/// <summary>
/// Image helper
/// </summary>
public static class ImageHelper
{
    /// <summary>
    /// Forced width
    /// </summary>
    private const int TargetWidth = 1819;

    /// <summary>
    /// Forced height
    /// </summary>
    private const int TargetHeight = 1311;

    /// <summary>
    /// Scale image to be compatible with the swiss post card creator
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static string Scale(byte[] data)
    {
        // Create magick image
        var image = new MagickImage(data);
        image.Format = MagickFormat.Jpeg;

        // Rotate when needed
        if(image.Width < image.Height) image.Rotate(90);

        // Scale as good as possible
        image.Scale(new MagickGeometry($"{TargetWidth}x{TargetHeight}^")
        {
            FillArea = true,
        });

        // Crop to target dimensions
        image.Crop(TargetWidth, TargetHeight, Gravity.Center);

        return image.ToBase64();
    }
}
