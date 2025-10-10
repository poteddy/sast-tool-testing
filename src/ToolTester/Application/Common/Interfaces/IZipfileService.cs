namespace ToolTester.Application.Common.Interfaces
{
    public interface IZipfileService
    {
         Dictionary<int, int> FileCount(string zipFileName);
        
    }
}