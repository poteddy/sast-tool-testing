using System.ComponentModel;
using System.Runtime.CompilerServices;
using ToolTester.Application.Relationships.DTO;
using ToolTester.Presentation.Interfaces;

namespace ToolTester.Presentation.Models
{
    public partial class Relationship:RelationshipDTO, IFromDto
    {
   

        #region IFromDto

        public void Init()
        {
            //put any code you'd want to exec after dto's been imported
            // for example to fill any new prop with data derived from what you received 

        }

        public void RaiseProperties()
        {
            var props = this.GetType().GetProperties();
            foreach (var property in props)
            {
                if (property.CanRead)
                {
                    OnPropertyChanged(property.Name);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

    }

}
