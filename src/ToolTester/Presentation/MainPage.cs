using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comet;


namespace ToolTester.Presentation
{
    public class MainPage : Comet.View
    {

        [State]
        readonly CometRide comet = new();

        [Body]
        Comet.View body()
            => new VStack {
                new Text(()=> $"({comet.Rides}) rides taken:{comet.CometTrain}")
                    .Frame(width:300)
                    .LineBreakMode(LineBreakMode.CharacterWrap),

                new Comet.Button("Ride the Comet! ☄️", ()=>{
                    comet.Rides++;
                })
                    .Frame(height:44)
                    .Margin(8)
                    .Color(Colors.White)
                    .Background(Colors.Green)
                .RoundedBorder(color:Colors.Blue)
                .Shadow(Colors.Grey,4,2,2),
            };

        public class CometRide : BindingObject
        {
            public int Rides
            {
                get => GetProperty<int>();
                set => SetProperty(value);
            }

            public string CometTrain
            {
                get
                {
                    return "☄️".Repeat(Rides);
                }
            }
        }
    }
}
