// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.
open System
open System.Windows.Forms
open System.Drawing
open MyControl


[<EntryPoint>]
[<STAThread>]
let main argv = 
    let f = new Form(Text="View", Width=716, Height=700, TopMost=false, MinimumSize=Size(500,500))
    let width = f.ClientSize.Width          //Dichiaro la variabile width assegnandole la larghezza della form
    let height = f.ClientSize.Height        //Dichiaro la variabile height assegnandole l'altezza della form
    let mc = new MyControl(Dock=DockStyle.Fill)

    
    //let myConv = new myConvolution(Dock=DockStyle.Fill, MyImage = new Bitmap(@"C:\Users\Paolo\Desktop\red.jpg"))
    //let myConv = new myConvolution(Dock=DockStyle.Fill, MyImage = new Bitmap(@"C:\Users\Paolo\Desktop\promo_5.jpg"))
    f.Controls.Add(mc)

    f.Show()
    Application.Run(f)

    0 // return an integer exit code
