namespace SvgToXamlConverter
{
    public record DrawingImage : Drawing
    {
        public Drawing? Drawing { get; }

        public DrawingImage(Drawing? drawing = null)
        {
            Drawing = drawing;
        }
    }
}
