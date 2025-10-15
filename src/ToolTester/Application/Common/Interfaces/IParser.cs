namespace ToolTester.Application.Common.Interfaces
{
    public interface IParser:IDisposable
    {        
        List<int> Get_findings(Stream fs);
     
    }
}