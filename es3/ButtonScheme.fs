module ButtonScheme
open System.Windows.Forms
open System.Drawing
open LWC
open MyButton
open MyConvolution

type Tool =
    | None = -1
    | FileList = 0
    | ZoomIn = 1
    | ZoomOut = 2
    | MoveUp = 3
    | MoveLeft =4
    | MoveRight = 5
    | MoveDown = 6
    | RotPos = 7
    | RotNeg = 8
    | Edge = 9
    | Sharpen = 10
    | GaussianBlur = 11

type ButtonScheme() as this =
    inherit LWC()
    let mutable toolSelected : Tool = Tool.None
    let mutable convRef:myConvolution = new myConvolution()
    let buttons = ResizeArray<MyButton>()
    let mutable kerMat = null

    let bt0 = new MyButton(Location=PointF(0.f,0.f), Size=SizeF(199.f, 30.f), Text = "File List", OnMouseDownListener = ( fun _ -> toolSelected<-Tool.FileList ))
    do buttons.Add(bt0)
    let bt1 = new MyButton(Location=PointF(0.f, 31.f), Size=SizeF(99.f,30.f), Text = "Zoom In", OnMouseDownListener = ( fun _ -> toolSelected<-Tool.ZoomIn ))
    do buttons.Add(bt1)
    let bt2 = new MyButton(Location=PointF(100.f, 31.f), Size=SizeF(99.f,30.f), Text = "Zoom Out", OnMouseDownListener = ( fun _ -> toolSelected<-Tool.ZoomOut))
    do buttons.Add(bt2)
    let bt3 = new MyButton(Location=PointF(0.f, 62.f), Size=SizeF(200.f,30.f), Text = "Up", OnMouseDownListener = ( fun _ -> toolSelected<-Tool.MoveUp))
    do buttons.Add(bt3)
    let bt4 = new MyButton(Location=PointF(0.f, 93.f), Size=SizeF(99.f,30.f), Text = "Left", OnMouseDownListener = ( fun _ -> toolSelected<-Tool.MoveLeft))
    do buttons.Add(bt4)
    let bt5 = new MyButton(Location=PointF(100.f, 93.f), Size=SizeF(99.f,30.f), Text = "Right", OnMouseDownListener = ( fun _ -> toolSelected<-Tool.MoveRight))
    do buttons.Add(bt5)
    let bt6 = new MyButton(Location=PointF(0.f, 124.f), Size=SizeF(200.f,30.f), Text = "Down", OnMouseDownListener = ( fun _ -> toolSelected<-Tool.MoveDown))
    do buttons.Add(bt6)
    let bt7 = new MyButton(Location=PointF(0.f, 155.f), Size=SizeF(99.f,30.f), Text = "Rot +", OnMouseDownListener = ( fun _ -> toolSelected<-Tool.RotPos))
    do buttons.Add(bt7)
    let bt8 = new MyButton(Location=PointF(100.f, 155.f), Size=SizeF(99.f,30.f), Text = "Rot -", OnMouseDownListener = ( fun _ -> toolSelected<-Tool.RotNeg))
    do buttons.Add(bt8)
    let bt9 = new MyButton(Location=PointF(0.f, 186.f), Size=SizeF(200.f,30.f), Text = "Edge", OnMouseDownListener = ( fun _ -> toolSelected<-Tool.Edge))
    do buttons.Add(bt9)
    let bt10 = new MyButton(Location=PointF(0.f, 217.f), Size=SizeF(200.f,30.f), Text = "Sharpen", OnMouseDownListener = ( fun _ -> toolSelected<-Tool.Sharpen))
    do buttons.Add(bt10)
    let bt11 = new MyButton(Location=PointF(0.f, 248.f), Size=SizeF(200.f,30.f), Text = "Blur", OnMouseDownListener = ( fun _ -> toolSelected<-Tool.GaussianBlur))
    do buttons.Add(bt11)


    let transformP (m:Drawing2D.Matrix) (p:Point) =
        let a = [| PointF(single p.X, single p.Y) |]
        m.TransformPoints(a)
        a.[0]

    let translateW (_w2v:Drawing2D.Matrix, _v2w:Drawing2D.Matrix, tx, ty) =
        _w2v.Translate(tx, ty)
        _v2w.Translate(-tx, -ty, Drawing2D.MatrixOrder.Append)

    let rotateW (_w2v:Drawing2D.Matrix, _v2w:Drawing2D.Matrix, a) =
        _w2v.Rotate a
        _v2w.Rotate(-a, Drawing2D.MatrixOrder.Append)

    let rotateAtW (_w2v:Drawing2D.Matrix, _v2w:Drawing2D.Matrix, p, a) =
        _w2v.RotateAt(a, p)
        _v2w.RotateAt(-a, p, Drawing2D.MatrixOrder.Append)

    let scaleW (_w2v:Drawing2D.Matrix, _v2w:Drawing2D.Matrix, sx, sy) =
        _w2v.Scale(sx, sy)
        _v2w.Scale(1.f/sx, 1.f/sy, Drawing2D.MatrixOrder.Append)

    let setKernel (s:string)=
        let mutable kernelMatrix = null      
        match s with
        | "edge" -> 
            kerMat <- array2D [ [ -1.f; -1.f; -1.f]; [-1.f; 8.f; -1.f]; [-1.f; -1.f; -1.f] ]
        | "sharpen" ->
            kerMat <- array2D [ [ 0.f; -1.f; 0.f]; [-1.f; 5.f; -1.f]; [0.f; -1.f; 0.f] ]
        | "boxblur" ->
            kerMat <- array2D [ [ 1.f/9.f; 1.f/9.f; 1.f/9.f]; [ 1.f/9.f; 1.f/9.f; 1.f/9.f]; [ 1.f/9.f; 1.f/9.f; 1.f/9.f] ]
        | "gaussianblur" ->
            kerMat <- array2D [ [ 1.f/16.f; 1.f/8.f; 1.f/16.f]; [1.f/8.f; 1.f/4.f; 1.f/8.f]; [ 1.f/16.f; 1.f/8.f; 1.f/16.f] ]
        | "emboss" ->
            kerMat <- array2D [[ -2.f; -1.f; 0.f]; [-1.f; 1.f; 1.f]; [0.f; 1.f; 2.f]]
        | _ -> 
            kerMat <- array2D [[ 0.f; 0.f; 0.f]; [0.f; 1.f; 0.f]; [0.f; 0.f; 0.f]]
    
    let redoFunc() =
        let rectArrayRef = convRef.RectangleArray
        let myImgRef = convRef.MyImage
        let myImgCopyRef = convRef.ImgCopy
        let gpRef = convRef.Gp
        let gRef = convRef.G
        let currPosRef = convRef.CurrentPosition
        let currDimRef = convRef.CurrentDimension
        let mutable iteration = 0

        for r in rectArrayRef do
            if(currPosRef = iteration) then
                for i=(int)r.X to (int)r.X+(int)r.Width do
                    for j=(int)r.Y to (int)r.Y+(int)r.Height do
                        myImgRef.SetPixel(i,j, myImgCopyRef.GetPixel(i,j))
            iteration <- iteration+1
        convRef.CurrentPosition <- convRef.CurrentPosition-1
        


    override this.OnMouseDown e =
        let mutable clicked = false
        let mutable v2wRef = convRef.V2w
        let mutable w2vRef = convRef.W2v
        for b in buttons do
            if b.HitTest (PointF (float32 e.X, float32 e.Y)) then
                b.OnMouseDown(e)      
        match toolSelected.ToString() with
        | "FileList" ->
            let dialogWindow = new OpenFileDialog()
            if ( dialogWindow.ShowDialog() = DialogResult.OK ) then
                convRef.MyImage <- new Bitmap(Image.FromFile(dialogWindow.FileName), new Size((int)convRef.Size.Width,(int)convRef.Size.Height))
                convRef.ImgCopy <- new Bitmap(Image.FromFile(dialogWindow.FileName), new Size((int)convRef.Size.Width,(int)convRef.Size.Height))
        | "ZoomIn" ->
            let p = transformP v2wRef (Point((int)(convRef.Size.Width/2.f), (int)(convRef.Size.Height/2.f)))
            scaleW(w2vRef, v2wRef, 1.1f, 1.1f)
        | "ZoomOut" ->
            let p = transformP v2wRef (Point((int)(convRef.Size.Width/2.f), (int)(convRef.Size.Height/2.f)))
            scaleW(w2vRef, v2wRef, 1.f/1.1f, 1.f/1.1f)
        | "MoveUp" -> 
            translateW(w2vRef, v2wRef, 0.f,-10.f)
        | "MoveLeft" -> 
            translateW(w2vRef, v2wRef, -10.f, 0.f)
        | "MoveRight" -> 
            translateW(w2vRef, v2wRef, 10.f, 0.f)
        | "MoveDown" -> 
            translateW(w2vRef, v2wRef, 0.f, 10.f)
        | "RotPos" -> 
            let p = transformP v2wRef (Point((int)(convRef.Size.Width/2.f), (int)(convRef.Size.Height/2.f)))
            rotateAtW (w2vRef, v2wRef, p, -10.f)
        | "RotNeg" -> 
            let p = transformP v2wRef (Point((int)(convRef.Size.Width/2.f), (int)(convRef.Size.Height/2.f)))
            rotateAtW (w2vRef, v2wRef, p, 10.f)
        | "Edge" ->
            setKernel("edge")
            convRef.KernelMatrix <- kerMat
        | "Sharpen" ->
            setKernel("sharpen")
            convRef.KernelMatrix <- kerMat
        | "GaussianBlur" ->
            setKernel("gaussianblur")
            convRef.KernelMatrix <- kerMat
        | _ -> ()

    override this.OnPaint(e:PaintEventArgs) =
        bt0.OnPaint e
        bt1.OnPaint e
        bt2.OnPaint e
        bt3.OnPaint e
        bt4.OnPaint e
        bt5.OnPaint e
        bt6.OnPaint e
        bt7.OnPaint e
        bt8.OnPaint e
        bt9.OnPaint e
        bt10.OnPaint e
        bt11.OnPaint e

    member this.ToolSelected
        with get() = toolSelected

    member this.ConvRef
        with get() = convRef
        and set(v) = convRef <- v; this.Invalidate()

