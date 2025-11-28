[System.Serializable]
public class Bool2D
{
    public int width;
    public int height;
    public bool[] data; // 1D로 저장 (width * height)

    public bool Get(int x, int y)
    {
        return data[y * width + x];
    }

    public void Set(int x, int y, bool value)
    {
        data[y * width + x] = value;
    }
}
