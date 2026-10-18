namespace ToolTester.Application.Common.Interfaces
{

    public interface IResult
    {
        string[] Errors { get; set; }
        string[] Warnings { get; set; }
        bool Succeeded { get; set; }

    }
    public interface IResult<out T> : IResult
    {
        T Data { get; }
    }

}
