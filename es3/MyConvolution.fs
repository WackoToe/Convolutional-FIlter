module MyConvolution
open System.Windows.Forms
open System.Drawing
open System.Drawing.Drawing2D
open System.Text
open LWC
open MyButton

type myConvolution() as this =
    inherit LWC()
    let mutable myImage:Bitmap= new Bitmap(100,100)
    let mutable imgCopy:Bitmap = new Bitmap(100,100)
    let mutable mouseDownClick: PointF = new PointF(0.f, 0.f)
    let mutable mouseMoveClick: PointF = new PointF(0.f, 0.f)
    let mutable mouseUpClick: PointF = new PointF(0.f,0.f)
    let mutable g = null
    let mutable rectLoc = new PointF(0.f, 0.f)
    let mutable rectWidth = 0.f
    let mutable rectHeight = 0.f
    let mutable mouseDown = false
    let mutable ellipse2Draw = false
    let mutable doubleUp = false
    let mutable gp = new GraphicsPath()
    let mutable kernelMatrix = array2D [ [ -1.f; -1.f; -1.f]; [-1.f; 8.f; -1.f]; [-1.f; -1.f; -1.f] ]
    let updateTime = 33
    let mutable myFactor = 0.f
    let mutable currentTime = 0.f
    let myTimer = new Timer(Interval = updateTime)
    let mutable timerOn = false
    
    let mutable rectangleArray = ResizeArray<RectangleF>()
    let mutable currentDimension = 0
    let mutable currentPosition = -1
    
    do myTimer.Tick.Add( fun _ ->
        myFactor <- myFactor + 1.f/(float32 updateTime)
        currentTime <- currentTime + 1.f/(float32 updateTime)
        if(currentTime>1.f) then
            myTimer.Stop()
            currentTime <- 0.f
            myFactor <- 0.f
            timerOn<-false
        this.Invalidate()
    )

    let mutable w2v = new Drawing2D.Matrix()
    let mutable v2w = new Drawing2D.Matrix()   
        
    let transformP (m:Drawing2D.Matrix) (p:Point) =
        let a = [| PointF(single p.X, single p.Y) |]
        m.TransformPoints(a)
        a.[0]

    let translateW (tx, ty) =
        w2v.Translate(tx, ty)
        v2w.Translate(-tx, -ty, Drawing2D.MatrixOrder.Append)

    let translate (x, y) =
        let t = [| PointF(0.f, 0.f); PointF(x, y) |]
        v2w.TransformPoints(t)
        translateW(t.[1].X - t.[0].X, t.[1].Y - t.[0].Y)

    let rotateW a =
        w2v.Rotate a
        v2w.Rotate(-a, Drawing2D.MatrixOrder.Append)

    let rotateAtW p a =
        w2v.RotateAt(a, p)
        v2w.RotateAt(-a, p, Drawing2D.MatrixOrder.Append)
        
    let setKernel (s:string)=      
        match s with
        | "edge" -> 
            kernelMatrix <- array2D [ [ -1.f; -1.f; -1.f]; [-1.f; 8.f; -1.f]; [-1.f; -1.f; -1.f] ]
        | "sharpen" ->
            kernelMatrix <- array2D [ [ 0.f; -1.f; 0.f]; [-1.f; 5.f; -1.f]; [0.f; -1.f; 0.f] ]
        | "boxblur" ->
            kernelMatrix <- array2D [ [ 1.f/9.f; 1.f/9.f; 1.f/9.f]; [ 1.f/9.f; 1.f/9.f; 1.f/9.f]; [ 1.f/9.f; 1.f/9.f; 1.f/9.f] ]
        | "gaussianblur" ->
            kernelMatrix <- array2D [ [ 1.f/16.f; 1.f/8.f; 1.f/16.f]; [1.f/8.f; 1.f/4.f; 1.f/8.f]; [ 1.f/16.f; 1.f/8.f; 1.f/16.f] ]
        | "emboss" ->
            kernelMatrix <- array2D [[ -2.f; -1.f; 0.f]; [-1.f; 1.f; 1.f]; [0.f; 1.f; 2.f]]
        | _ -> 
            kernelMatrix <- array2D [[ 0.f; 0.f; 0.f]; [0.f; 1.f; 0.f]; [0.f; 0.f; 0.f]]

    let printBytes b =
        printfn "%s" (Encoding.ASCII.GetString(b))

    let calculatePixel (i:int, j:int) = 
        let mutable oldColor : Color = Unchecked.defaultof<Color>
        oldColor <- imgCopy.GetPixel(i,j)
        let oldRed = (float32)oldColor.R
        let oldGreen = (float32)oldColor.G
        let oldBlue = (float32)oldColor.B 
        let mutable newRed = 0.f
        let mutable newGreen = 0.f
        let mutable newBlue = 0.f
        //printfn "%f" myFactor
        
        for x=i-1 to i+1 do
            for y=j-1 to j+1 do
                newRed <- (newRed+ kernelMatrix.[x-i+1, y-j+1]* (float32)(imgCopy.GetPixel(x,y).R)) //* myFactor + (1.f-myFactor)*oldRed
                newGreen <- (newGreen+ kernelMatrix.[x-i+1, y-j+1]*(float32) (imgCopy.GetPixel(x,y).G)) //* myFactor + (1.f-myFactor)*oldGreen
                newBlue <- (newBlue+ kernelMatrix.[x-i+1, y-j+1]* (float32) (imgCopy.GetPixel(x,y).B)) //* myFactor + (1.f-myFactor)*oldBlue
        if(newRed <0.f) then
            newRed<-0.f
        if(newGreen <0.f) then
            newGreen<-0.f
        if(newBlue<0.f) then
            newBlue<-0.f

        if(newRed>255.f) then
            newRed<-255.f
        if(newGreen>255.f) then
            newGreen<-255.f
        if(newBlue>255.f) then
            newBlue<-255.f
        
        let mutable newColor = Color.FromArgb(255, (int)newRed, (int)newGreen, (int)newBlue)
        myImage.SetPixel(i,j, newColor)

               
    let applyKernel() =
        for i=(int)rectLoc.X to (int)rectLoc.X+(int)rectWidth do
            for j=(int)rectLoc.Y to (int)rectLoc.Y+(int)rectHeight do
                if(gp.IsVisible(i,j)) then
                    //printfn "%f" myFactor
                    //let transformI = transformP v2w (Point(i,j))
                    //calculatePixel((int)transformI.X, (int)transformI.Y)
                    calculatePixel(i,j)

    override this.OnMouseDown e =
        mouseDownClick <- new PointF((float32)e.X, (float32)e.Y)
        mouseDown <- true
        doubleUp <- true
        
    override this.OnMouseMove e =    
        if(mouseDown) then
            mouseMoveClick <- new PointF((float32)e.X, (float32)e.Y)
            let mutable temp = 0.f
            if(mouseMoveClick.X < mouseDownClick.X) then
                temp <- mouseDownClick.X
                mouseDownClick.X <- mouseMoveClick.X
                mouseMoveClick.X <- temp
            if(mouseMoveClick.Y < mouseDownClick.Y) then
                temp <- mouseDownClick.Y
                mouseDownClick.Y <- mouseMoveClick.Y
                mouseMoveClick.Y <- temp
            
            let moveTransformed = transformP v2w (Point(int(mouseMoveClick.X),int(mouseMoveClick.Y)))
            let downTransformed = transformP v2w (Point(int(mouseDownClick.X),int(mouseDownClick.Y)))
            rectLoc <- downTransformed
            rectWidth <- moveTransformed.X - downTransformed.X
            rectHeight <- moveTransformed.Y - downTransformed.Y

            (*rectLoc <- mouseDownClick
            rectWidth <- mouseMoveClick.X - mouseDownClick.X
            rectHeight <- mouseMoveClick.Y - mouseDownClick.Y*)
            ellipse2Draw <-true
            this.Invalidate()

    override this.OnMouseUp e =
        if(doubleUp) then
            mouseUpClick <- new PointF((float32)e.X, (float32)e.Y)
            let mutable temp = 0.f
            if(mouseUpClick.X < mouseDownClick.X) then
                temp <- mouseDownClick.X
                mouseDownClick.X <- mouseUpClick.X
                mouseUpClick.X <- temp
            if(mouseUpClick.Y < mouseDownClick.Y) then
                temp <- mouseDownClick.Y
                mouseDownClick.Y <- mouseUpClick.Y
                mouseUpClick.Y <- temp

            let upTransformed = transformP v2w (Point(int(mouseUpClick.X),int(mouseUpClick.Y)))
            let downTransformed = transformP v2w (Point(int(mouseDownClick.X),int(mouseDownClick.Y)))
            rectLoc <- downTransformed
            rectWidth <- upTransformed.X - downTransformed.X
            rectHeight <- upTransformed.Y - downTransformed.Y

            (*rectLoc <- mouseDownClick
            rectWidth <- mouseUpClick.X - mouseDownClick.X
            rectHeight <- mouseUpClick.Y - mouseDownClick.Y*)
            
            let rect = new RectangleF(rectLoc, new SizeF(rectWidth, rectHeight))
            gp.AddEllipse(rect)
            mouseDown <- false
            if(rectLoc.X+rectWidth<this.Size.Width && rectLoc.X+rectWidth >0.f && rectLoc.Y+rectHeight<this.Size.Height && rectLoc.Y + rectHeight> 0.f) then
                ellipse2Draw <-true
            else
                ellipse2Draw <- false
            doubleUp <- false
            myTimer.Start()
            timerOn <- true
            if(currentDimension = currentPosition+1) then     
                rectangleArray.Add(rect)
                currentPosition <- currentPosition + 1
                currentDimension <- currentDimension + 1
            else
                currentPosition <- currentPosition+1
                rectangleArray.Insert(currentPosition, rect)
                for i=currentPosition+1 to currentDimension do
                    rectangleArray.RemoveAt(i)
                currentDimension <- currentPosition+1
            this.Invalidate()


    override this.OnPaint(e:PaintEventArgs) =
        g <- e.Graphics      
        g.Transform <- w2v
        if(myImage <> null) then
            g.DrawImage(myImage, 0, 0)
        if(ellipse2Draw || timerOn) then
            let rect = new RectangleF(rectLoc, new SizeF(rectWidth, rectHeight))
            g.DrawEllipse(Pens.Red, rect)
            g.SetClip(gp)
            if(not(mouseDown)) then
                applyKernel()
            ellipse2Draw <- false
        if mouseDown then
            gp.Reset()
        
    member this.MyImage
        with get() = myImage
        and set(v) = myImage <- v; this.Invalidate()

    member this.ImgCopy
        with get() = imgCopy
        and set(v) = imgCopy <- v; this.Invalidate()

    member this.W2v 
        with get() = w2v
        and set(v) = w2v <- v; this.Invalidate()

    member this.V2w
        with get() = v2w
        and set(v) = v2w <- v; this.Invalidate()

    member this.Gp
        with get() = gp
        and set(v) = gp <- v

    member this.KernelMatrix
        with get() = kernelMatrix
        and set(v) = kernelMatrix <- v

    member this.RectangleArray
        with get() = rectangleArray
        and set(v) = rectangleArray <- v

    member this.CurrentPosition
        with get() = currentPosition
        and set(v) = currentPosition <- v

    member this.CurrentDimension
        with get() = currentDimension
        and set(v) = currentDimension <-v

    member this.G
        with get() = g
        and set(v) = g<-v
