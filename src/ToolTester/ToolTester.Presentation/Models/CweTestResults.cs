using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ToolTester.Application.CWETestResultBases.DTO;
using ToolTester.Application.JulietCoeverages.DTO;
using ToolTester.Application.Reports.DTO;
using ToolTester.Presentation.Interfaces;

namespace ToolTester.Presentation.Models
{
    public class RelatedItemsInTest : IFromDto
    {

        public int Id { get; set; }

        public int ScanId { get; set; }

        public int ToolId { get; set; }

        public int GroundTruthCweId { get; set; }

        public int ScannerCweId { get; set; }

        public int? RootCauseCweId { get; set; }

        public bool RootCauseMatchesGroundTruth { get; set; }

        public bool RootCauseMatchesScanner { get; set; }

        public int Count { get; set; }

        public string Relationship { get; set; } =
            string.Empty;

        public int RelationshipScore { get; set; }

        public string TopologyRelationship { get; set; } =
            string.Empty;

        public string GroundTruthAbstraction { get; set; } =
            string.Empty;

        public string ScannerAbstraction { get; set; } =
            string.Empty;

        public int? TopologyDistance { get; set; }

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
