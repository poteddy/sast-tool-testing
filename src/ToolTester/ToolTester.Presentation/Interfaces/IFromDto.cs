using System.ComponentModel;

namespace ToolTester.Presentation.Interfaces
{
    public interface IFromDto : INotifyPropertyChanged
    {
        //
        // Summary:
        //     Can initialize model after it's being loaded from dto
        void Init();

        //
        // Summary:
        //     Notify all properties were updated
        void RaiseProperties();
    }
}
