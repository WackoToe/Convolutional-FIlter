module LWContainer
open System.Windows.Forms
open System.Drawing
open LWC
open MyButton

type LWContainer() as this =
  inherit UserControl()

  
  let controls = ResizeArray<LWC>()


  let cloneMouseEvent (c:LWC) (e:MouseEventArgs) =
    new MouseEventArgs(e.Button, e.Clicks, e.X - int(c.Location.X), e.Y - int(c.Location.Y), e.Delta)

  let correlate (e:MouseEventArgs) (f:LWC->MouseEventArgs->unit) =
    let mutable found = false
    for i in { (controls.Count - 1) .. -1 .. 0 } do
      if not found then
        let c = controls.[i]
        if c.HitTest(PointF(single(e.X) - c.Location.X, single(e.Y) - c.Location.Y)) then
          found <- true
          f c (cloneMouseEvent c e)



  let mutable captured : LWC option = None

  do this.DoubleBuffered <- true

  member this.LWControls
    with get() = controls

  override this.OnMouseDown e =
    correlate e (fun c ev -> captured <- Some(c); c.OnMouseDown(ev))
    base.OnMouseDown e

  override this.OnMouseUp e =
    correlate e (fun c ev -> c.OnMouseUp(ev))
    match captured with
    | Some c -> c.OnMouseUp(cloneMouseEvent c e); captured <- None;  
    | None  -> ()
    base.OnMouseUp e

  override this.OnMouseMove e =
    correlate e (fun c ev -> c.OnMouseMove(ev))
    match captured with
    | Some c -> c.OnMouseMove(cloneMouseEvent c e)
    | None  -> ()
    base.OnMouseMove e

  override this.OnPaint e =
    controls |> Seq.iter (fun c ->
      let s = e.Graphics.Save()
      e.Graphics.TranslateTransform(c.Location.X, c.Location.Y)
      e.Graphics.Clip <- new Region(RectangleF(0.f, 0.f, c.Size.Width, c.Size.Height))
      let r = e.Graphics.ClipBounds
      let evt = new PaintEventArgs(e.Graphics, new Rectangle(int(r.Left), int(r.Top), int(r.Width), int(r.Height)))
      c.OnPaint evt
      e.Graphics.Restore(s)
    )
    base.OnPaint(e)

