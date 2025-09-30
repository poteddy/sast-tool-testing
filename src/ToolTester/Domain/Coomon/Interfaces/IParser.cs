namespace ToolTester.Domain.Coomon.Interfaces
{
    public interface IParser:IDisposable
    {        
        List<int> Get_findings(Stream fs);
     
    }
}