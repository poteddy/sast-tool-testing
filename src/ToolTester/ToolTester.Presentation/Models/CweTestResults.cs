using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ToolTester.Application.CWETestResultBases.DTO;
using ToolTester.Application.JulietCoeverages.DTO;
using ToolTester.Presentation.Interfaces;

namespace ToolTester.Presentation.Models
{
    public class CweTestResults : CweTestResultBaseDTO, IFromDto
    {
        
        public double ErrorValue {  get; set; }


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
