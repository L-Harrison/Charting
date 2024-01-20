using Charting.Extensions;
using Charting.Models;

using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Charting.Test
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window,INotifyPropertyChanged
    {
       
        public event PropertyChangedEventHandler? PropertyChanged;
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval=TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();

            DispatcherTimer timer1 = new DispatcherTimer();
            timer1.Interval = TimeSpan.FromSeconds(10);
            timer1.Tick += Timer_Tick1;
            timer1.Start();

            #region 

            LastTime = 3600;
            GradientX = new double[10];
            GradientY = new double[10];
            SpeedX = new double[10];
            SpeedY = new double[10];

            GradientRealX =new double[OscillogramCharting.AllNumConst];
            GradientRealY=new double[OscillogramCharting.AllNumConst];
            SpeedRealX = new double[OscillogramCharting.AllNumConst];
            SpeedRealY = new double[OscillogramCharting.AllNumConst];
            PressureX = new double[OscillogramCharting.AllNumConst];
            PressureY = new double[OscillogramCharting.AllNumConst];

            (X,Y) = OscillogramExtension.InitilzeXY(new double[] { 254, 300, 400, 450 });

            GradientX[0] = 0;
            GradientY[0] = 20;

            GradientX[1] = 200;
            GradientY[1] = 200;
            GradientX[2] = 300;
            GradientY[2] = 300;
            GradientX[3] = 400;
            GradientY[3] = 400;
            GradientX[4] = 500;
            GradientY[4] = 400;
            GradientX[5] = 600;
            GradientY[5] = 800;
            GradientX[6] = 700;
            GradientY[6] = 800;
            GradientX[7] = 800;
            GradientY[7] = 900;
            GradientX[8] = 900;
            GradientY[8] = 600;
            GradientX[9] = lastTime;
            GradientY[9] = 600;


            SpeedX[0] = 0;
            SpeedY[0] = 200;
            SpeedX[1] = 200;
            SpeedY[1] = 200;
            SpeedX[2] = 300;
            SpeedY[2] = 300;
            SpeedX[3] = 400;
            SpeedY[3] = 300;
            SpeedX[4] = 500;
            SpeedY[4] = 300;
            SpeedX[5] = 600;
            SpeedY[5] = 350;
            SpeedX[6] = 700;
            SpeedY[6] = 350;
            SpeedX[7] = 800;
            SpeedY[7] = 300;
            SpeedX[8] = 900;
            SpeedY[8] = 300;
            SpeedX[9] = lastTime;
            SpeedY[9] = 600;
            

            #endregion
        }
        int wave = 451;
        private void Timer_Tick1(object? sender, EventArgs e)
        {
            return;
            var x = new double[CurrentTime];
            var y = new double[CurrentTime];
            for (int i = 0; i < CurrentTime; i++)
            {
                x[i] = (double)i;
                y[i] = Math.Cos(i % 90) * 1000 * i;
            }
            (X, Y).AppendXY(wave++, x, y);
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (CurrentTime > LastTime)
                return;

            if (true)
            {
                gradientRealX[CurrentTime] = CurrentTime;
                gradientRealY[CurrentTime] = Math.Log(CurrentTime + 1) * 100;

                SpeedRealX[CurrentTime] = CurrentTime;
                SpeedRealY[CurrentTime] = -Math.Log(CurrentTime + 1) * 80;

                PressureX[CurrentTime] = CurrentTime;
                PressureY[CurrentTime] = Math.Log2(CurrentTime + 1) * 80;

                for (int i = 0; i < X.Count; i++)
                {
                    X[i].V[CurrentTime] = CurrentTime;
                    Y[i].V[CurrentTime] = Math.Sin(CurrentTime % 90) * 100 * i;
                }
            }

            CurrentTime++;
         
        }



        private bool enableEditDrag;

        public bool EnableEditDrag
        {
            get { return enableEditDrag; }
            set { enableEditDrag = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("EnableEditDrag"));
            }
        }


        private int currentTime;

        public int CurrentTime
        {
            get { return currentTime; }
            set { currentTime = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentTime"));
            }
        }

        private int lastTime;

        public int LastTime
        {
            get { return lastTime; }
            set
            {
                lastTime = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("LastTime"));
            }
        }

        #region gradient
        private double[] gradientX;

        public double[] GradientX
        {
            get { return gradientX; }
            set
            {
                gradientX = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("GradientX"));
            }
        }

        private double[] gradientY;

        public double[] GradientY
        {
            get { return gradientY; }
            set
            {
                gradientY = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("GradientY"));
            }
        } 
        #endregion

        #region speed

        private double[] speedX;

        public double[] SpeedX
        {
            get { return speedX; }
            set
            {
                speedX = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SpeedX"));
            }
        }

        private double[] speedY;

        public double[] SpeedY
        {
            get { return speedY; }
            set
            {
                speedY = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SpeedY"));
            }
        } 
        #endregion

        #region GradientReal
        private double[] gradientRealX;

        public double[] GradientRealX
        {
            get { return gradientRealX; }
            set
            {
                gradientRealX = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("GradientRealX"));
            }
        }

        private double[] gradientRealY;

        public double[] GradientRealY
        {
            get { return gradientRealY; }
            set
            {
                gradientRealY = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("GradientRealY"));
            }
        }

        #endregion

        #region speedReal
        private double[] speedRealX;

        public double[] SpeedRealX
        {
            get { return speedRealX; }
            set
            {
                speedRealX = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SpeedRealX"));
            }
        }

        private double[] speedRealY;

        public double[] SpeedRealY
        {
            get { return speedRealY; }
            set
            {
                speedRealY = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SpeedRealY"));
            }
        } 
        #endregion

        #region    pressure 
        private double[] pressureX;

        public double[] PressureX
        {
            get { return pressureX; }
            set
            {
                pressureX = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PressureX"));
            }
        }

        private double[] pressureY;

        public double[] PressureY
        {
            get { return pressureY; }
            set
            {
                pressureY = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PressureY"));
            }
        } 
        #endregion

        #region XY

        private ObservableCollection<OscillogramWave> x;

        public ObservableCollection<OscillogramWave> X
        {
            get { return x; }
            set
            {
                x = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Y"));
            }
        }

        private ObservableCollection<OscillogramWave> y;

        public ObservableCollection<OscillogramWave> Y
        {
            get { return y; }
            set
            {
                y = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Y"));
            }
        } 
        #endregion
    }
}