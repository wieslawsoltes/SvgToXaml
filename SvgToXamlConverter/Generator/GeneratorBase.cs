namespace SvgToXamlConverter
{
    public abstract class GeneratorBase
    {
        public abstract string Generate(Brush brush, GeneratorContext context);

        public abstract string Generate(SolidColorBrush solidColorBrush, GeneratorContext context);

        public abstract string Generate(LinearGradientBrush linearGradientBrush, GeneratorContext context);

        public abstract string Generate(RadialGradientBrush radialGradientBrush, GeneratorContext context);

        public abstract string Generate(TwoPointConicalGradientBrush twoPointConicalGradientBrush, GeneratorContext context);

        public abstract string Generate(PictureBrush pictureBrush, GeneratorContext context);

        public abstract string Generate(Pen pen, GeneratorContext context);

        public abstract string Generate(Drawing drawing, GeneratorContext context);

        public abstract string Generate(GeometryDrawing geometryDrawing, GeneratorContext context);

        public abstract string Generate(DrawingGroup drawingGroup, GeneratorContext context);

        public abstract string Generate(DrawingImage drawingImage, GeneratorContext context);

        public abstract string Generate(Resource resource, GeneratorContext context);

        public abstract string Generate(ResourceDictionary resourceDictionary, GeneratorContext context);

        public abstract string Generate(Image image, GeneratorContext context);

        public abstract string Generate(Styles styles, GeneratorContext context);
    }
}
