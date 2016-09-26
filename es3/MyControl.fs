module MyControl
open LWContainer
open System.Windows.Forms
open System.Drawing
open LWC
open MyConvolution
open ButtonScheme

type MyControl() as this =
    inherit LWContainer()
    do this.DoubleBuffered<-true
    //let img1 = new Bitmap(Image.FromFile("C:\Users\Paolo\Pictures\Foto\Foto USA 3\save_img_040 - Copia.jpg"), new Size(500, 700))
    //let img2 = new Bitmap(Image.FromFile("C:\Users\Paolo\Pictures\Foto\Foto USA 3\save_img_040 - Copia.jpg"), new Size(500, 700))
    let img1 = null
    let img2 = null
    let myConv = new myConvolution(Parent = this, Size = SizeF(500.f, 700.f), MyImage = img1, ImgCopy = img2)
    let bs = new ButtonScheme(Parent = this, Location = PointF(500.f,0.f), Size = SizeF(200.f, 350.f), ConvRef=myConv)
    let mutable myConvBool = false

    do base.LWControls.Add(myConv)
    do base.LWControls.Add(bs)

    override this.OnMouseDown e = 
        base.OnMouseDown e 
        this.Invalidate()

    (*override this.OnMouseMove e = 
        base.OnMouseMove e 
        this.Invalidate()

    override this.OnMouseUp e = 
        base.OnMouseUp e 
        this.Invalidate()*)

    override this.OnPaint(e:PaintEventArgs) =
        base.OnPaint e