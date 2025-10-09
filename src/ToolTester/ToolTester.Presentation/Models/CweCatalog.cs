using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ToolTester.Application.CWECataloig.DTO;
using ToolTester.Presentation.Interfaces;

namespace ToolTester.Presentation.Models
{
    public class CweCatalog:CWECatalogDTO,IFromDto
    {
        // we want to be able to change this Name property in run-time and to 
        // reflect changes so we make it bindable (other props will remain without 
        // OnPropertyChanged BUT we can always update all bindings in code if needed 
        // using RaiseProperties();):

        private string _name;
        public new string Name
        {
            get { return _name; }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        // ADD any properties you need for UI
        // ...

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
