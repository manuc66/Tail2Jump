namespace PartialView
{
  public class CharSize
  {
    private readonly double _width;
    private readonly double _height;

    public CharSize(double width, double height)
    {
      _width = width;
      _height = height;
    }

    public double Width
    {
      get { return _width; }
    }

    public double Height
    {
      get { return _height; }
    }
  }
}