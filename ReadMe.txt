This project is for converting color space of images from RGB to CMYK based on several pre-defined color profiles. If the source image has no profile, it is accepted as 24 bit RGB.
Conversion is done by buit-in .NET 4.0+ library System.Windows.Media, using the class ColorConvertedBitmap.
User interface is WPF based, except for Windows.Forms.OpenFolderDialog, which has no equivalent in WPF yet.