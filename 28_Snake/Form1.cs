// step 0: adapt TargetFrameworks of .net10.0 in properties of project: .net10.0-windows10.0.19041.0
// step 1: import RawGameCOntroller with using
using Windows.Gaming.Input;
using Timer = System.Windows.Forms.Timer;



namespace _28_Snake
{
    public partial class Form1 : Form
    {
        const int Rows = 29;
        const int Cols = 29;
        const int ElemSize = 20;
        const int StartMovingInterval = 100;
        const int StepsToFirstApple = Cols / 4;

        readonly static Random rng = new Random();

        // Snake
        Panel head = new Panel() { BackColor = Color.Green, Size = new Size(ElemSize, ElemSize) };
        Queue<Panel> tail = new Queue<Panel>();
        MovingDirections snakeMovingDirection = MovingDirections.None;

        // Futter
        Panel apple = new Panel() { BackColor = Color.Red, Size = new Size(ElemSize, ElemSize)};

        Timer tmrMoveSnake = new Timer() { Interval = StartMovingInterval, Enabled = true };

        // step 2: add field for game controller and timer for polling input data of controller
        RawGameController? controller = null;
        Timer tmrPollingControllerStatus = new Timer() { Interval = 16, Enabled = true };

        public Form1()
        {
            // Raster des Spiels erzeugen --> Grösse des Spielfeldes an den Raster anpassen
            Text = "Snake";
            Width = Cols * ElemSize + (Width - ClientSize.Width);
            Height = Rows * ElemSize + (Height - ClientSize.Height); // Rand muss zum Raster dazugezählt werden

            // Snake mittig positionieren
            GameInit();

            Controls.Add(head);
            Controls.Add(apple);

            // Event-Handler für Tastatureingaben und Timer
            KeyDown += Form1_KeyDown;
            tmrMoveSnake.Tick += TmrMoveSnake_Tick;

            // step 3: register for controller events and tmrPolligControllerStatus
            RawGameController.RawGameControllerAdded += RawGameController_RawGameControllerAdded;
            RawGameController.RawGameControllerRemoved += RawGameController_RawGameControllerRemoved;

            tmrPollingControllerStatus.Tick += TmrPollingControllerStatus_Tick;
        }

        private void TmrPollingControllerStatus_Tick(object? sender, EventArgs e)
        {
            if (controller != null)
            {
                int numOfAxes = Math.Max(0, controller.AxisCount);
                int numOfButtons = Math.Max(0, controller.ButtonCount);

                double[] axesValues = new double[numOfAxes];

                // Auslesen des Aktuelles Status des Controllers: Buttons, Switches, Achsen-Werte auslesen
                controller.GetCurrentReading(null, null, axesValues);

                if (numOfAxes > 1)
                {
                    axesValues[0] = NormalizeAxisValue(axesValues[0]);
                    axesValues[1] = NormalizeAxisValue(axesValues[1]);
                    // -1, d.h. nach Links
                    // -0.3 bis 0.3 --> 0, d.h. nichts Gedrückt
                    // 1, d.h. nach Rechts
                    double deadZone = 0.3;
                    //if (axesValues[0] > -deadZone && axesValues[0] < deadZone)
                    //    axesValues[0] = 0;
                    //if (axesValues[1] > -deadZone && axesValues[1] < deadZone)
                    //    axesValues[1] = 0;

                    if (axesValues[0] < -deadZone)
                        snakeMovingDirection = MovingDirections.Left;
                    if (axesValues[0] > deadZone)
                        snakeMovingDirection = MovingDirections.Right;
                    if (axesValues[1] < -deadZone)
                        snakeMovingDirection = MovingDirections.Up;
                    if (axesValues[1] > deadZone)
                        snakeMovingDirection = MovingDirections.Down;
                }
            }
        }

        private double NormalizeAxisValue(double value)
        {
            if (double.IsNaN(value))    // NaN --> Is not a Number, d.h. kein gültiger double Wert
                return 0;

            if (value >= 0 && value <= 1)
                return value * 2 - 1; // 0 ... 1 --> -1 ... 1

            if (value < 0)
                return -1;

            if (value > 1)
                return 1;

            return value; // Unverändert zurückgeben, falls es außerhalb des erwarteten Bereichs liegt
        }


        private void RawGameController_RawGameControllerRemoved(object? sender, RawGameController e)
        {
            controller = null;
        }

        private void RawGameController_RawGameControllerAdded(object? sender, RawGameController e)
        {
            controller = e;
        }

        private void GameInit()
        {
            head.Location = new Point((Cols / 2) * ElemSize, (Rows / 2) * ElemSize);
            snakeMovingDirection = MovingDirections.None;

            apple.Location = new Point((Cols / 2 + StepsToFirstApple) * ElemSize, (Rows / 2) * ElemSize);

            Controls.Add(head);
            Controls.Add(apple);
        }

        private void TmrMoveSnake_Tick(object? sender, EventArgs e) //Game Loop
        {
            // GameOver: Prüfen, ob der Kopf der Schlange gegen eine Wand fahren würde
            if (head.Top == 0 && snakeMovingDirection == MovingDirections.Up || 
                head.Bottom == ClientSize.Height && snakeMovingDirection == MovingDirections.Down || 
                head.Left == 0 && snakeMovingDirection == MovingDirections.Left || 
                head.Right == ClientSize.Width && snakeMovingDirection == MovingDirections.Right)
            {
                GameOver();
            }

            // Prüfen, ob sich die Schlange auf dem Apfel befindet
            if (head.Location == apple.Location)
            {
                // ein neues Schwanz elemenz erzeugen
                Panel tailElem = new Panel() { 
                    BackColor = Color.LightGreen,
                    Size = new Size(ElemSize, ElemSize),
                    Location = head.Location
                };
                tail.Enqueue(tailElem);
                Controls.Add(tailElem);
                
                // Futter ein eine neue Position setzen
                apple.Location = new Point(rng.Next(0, Cols) * ElemSize, rng.Next(0, Rows) * ElemSize);
            }

            // Letztes Schwanzelement an die aktuelle Position des Kopfe bewegen
            if (tail.Count > 0)
            {
                Panel lastTailElement = tail.Dequeue();
                lastTailElement.Location = head.Location;
                tail.Enqueue(lastTailElement); 
            }
            

            MoveSnakeHead();

            // GameOver: Prüfen, ob der Kopf der Schlange gegen den Schwanz fahren würde
            foreach (Panel tailElement in new Queue<Panel>(tail))
            {
                if (tailElement.Location == head.Location)
                {
                    GameOver();
                }
            }
        }

        private void GameOver()
        {
            tmrMoveSnake.Stop();
            DialogResult dialogResult = MessageBox.Show("Nochmals?", "Game Over!", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                Controls.Clear();
                tail.Clear();

                GameInit();
                tmrMoveSnake.Start();
            }
            else
            {
                Close();
            }
        }

        private void MoveSnakeHead()
        {
            switch (snakeMovingDirection)
            {
                case MovingDirections.Up:
                    head.Top -= ElemSize;
                    break;
                case MovingDirections.Down:
                    head.Top += ElemSize;
                    break;
                case MovingDirections.Left:
                    head.Left -= ElemSize;
                    break;
                case MovingDirections.Right:
                    head.Left += ElemSize;
                    break;
                default:
                    break;
            }
        }

        private void Form1_KeyDown(object? sender, KeyEventArgs e)
        {
            switch (e.KeyCode)  // Bewegungsrichtung wird eingestellt wenn eine Taste gedrückt wird
            {
                case Keys.Up:
                    if (snakeMovingDirection != MovingDirections.Down)
                        snakeMovingDirection = MovingDirections.Up;
                    break;
                case Keys.Down:
                    if (snakeMovingDirection != MovingDirections.Up)
                        snakeMovingDirection = MovingDirections.Down;
                    break;
                case Keys.Left:
                    if (snakeMovingDirection != MovingDirections.Right)
                        snakeMovingDirection = MovingDirections.Left;
                    break;
                case Keys.Right:
                    if (snakeMovingDirection != MovingDirections.Left)
                        snakeMovingDirection = MovingDirections.Right;
                    break;
            }
        }
    }
}