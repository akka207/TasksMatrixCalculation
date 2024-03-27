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

namespace _2024._03._25
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            pBar1.Value = 0;
            pBar2.Value = 0;

            int dim = Convert.ToInt32(textBox.Text);
            double[,] a = GenerateMatrix(dim);
            double[,] b = GenerateMatrix(dim);

            Action<int> progress1 = new Action<int>((int a) =>
            {
                int progress = a;
                Dispatcher.Invoke(() =>
                {
                    pBar1.Value = (progress * 100) / dim;
                });
            });
            Action<int> progress2 = new Action<int>((int a) =>
            {
                int progress = a;
                Dispatcher.Invoke(() =>
                {
                    pBar2.Value = (progress * 100) / dim;
                });
            });
            Action<int> progress3 = new Action<int>((int a) =>
            {
                int progress = a;
                Dispatcher.Invoke(() =>
                {
                    pBar3.Value = (progress * 100) / dim;
                });
            });

            List<Thread> threads = new List<Thread>();

            threads.Add(new Thread(() =>
            {
                double[,] res = MultipleMatrix(dim, a, b, progress1);
            }));
            threads.Add(new Thread(() =>
            {
                double[,] res = DeviceThreading(dim, a, b, progress2);
            }));
            threads.Add(new Thread(() =>
            {
                double[,] res = RowsThreading(dim, a, b, progress3);
            }));
            threads.Add(new Thread(() =>
            {
                double[,] res = CellsThreading(dim, a, b, (int a) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        pBar4.Value = a;
                    });
                });
            }));

            //threads[0].Priority = ThreadPriority.Highest;

            //foreach (Thread t in threads)
            //{
            //    t.Start();
            //}

            //threads[3].Start();

            var res = Task<double[,]>.Run(() =>
            {
                return CellsThreading(dim, a, b, (int a) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        pBar4.Value = a;
                    });
                });
            });
        }


        private void MultiplreOneElement(object? param)
        {
            if (param == null)
                throw new ArgumentNullException(nameof(param));
            MatrixParams matrixParams = (MatrixParams)param;
            double result = 0;
            for (int mi = 0; mi < matrixParams.dim; mi++)
            {
                result = result + matrixParams.a[matrixParams.i, mi] * matrixParams.b[mi, matrixParams.j];
            }
            matrixParams.c[matrixParams.i, matrixParams.j] = result;
        }
        private static double MultipleOneElement(int dim, int i, int j, double[,] a, double[,] b, Action? action)
        {
            double result = 0;
            for (int mi = 0; mi < dim; mi++)
            {
                result = result + a[i, mi] * b[mi, j];
            }

            action?.Invoke();

            return result;
        }
        private double[,] GenerateMatrix(int dim)
        {
            var result = new double[dim, dim];
            for (int i = 0; i < dim; i++)
            {
                for (int j = 0; j < dim; j++)
                {
                    result[i, j] = new Random().NextDouble() * 40 - 20;
                }
            }
            return result;
        }

        private double[,] MultipleMatrix(int dim, double[,] a, double[,] b, Action<int> progress)
        {
            int count = 0;
            var result = new double[dim, dim];
            for (int i = 0; i < dim; i++)
            {
                progress?.Invoke(i);
                for (int j = 0; j < dim; j++)
                {
                    result[i, j] = MultipleOneElement(dim, i, j, a, b, () =>
                    {
                        lock (obj1)
                        {
                            count++;
                            progress?.Invoke(count);
                        }
                    });
                }
            }
            return result;
        }


        private object obj1 = new object();
        private double[,] DeviceThreading(int dim, double[,] a, double[,] b, Action<int> progress)
        {
            int count = 0;

            Action subProgress = () => 
            {
                lock (obj1)
                {
                    count++;
                    progress?.Invoke(count);
                }
            };

            int taskCount = 8;
            var result = new double[dim, dim];
            Task[] tasks = new Task[taskCount];
            for (int t = 0; t < taskCount; t++)
            {
                var p = t;
                tasks[t] = Task.Run(() =>
                {
                    for (int i = (dim / taskCount) * p; i < (dim / taskCount) * (p + 1); i++)
                    {
                        for (int j = 0; j < dim; j++)
                        {
                            result[i, j] = MultipleOneElement(dim, i, j, a, b, subProgress);
                        }
                    }
                });
            }

            for (int t = 0; t < taskCount; t++)
            {
                tasks[t].Wait();
            }

            return result;
        }


        private object obj2 = new object();
        private double[,] RowsThreading(int dim, double[,] a, double[,] b, Action<int> progress)
        {
            int count = 0;

            Action subProgress = () =>
            {
                lock (obj2)
                {
                    count++;
                    progress?.Invoke(count);
                }
            };

            var result = new double[dim, dim];
            Task[] tasks = new Task[dim];

            for (int i = 0; i < dim; i++)
            {
                var mi = i;
                tasks[i] = Task.Run(() =>
                {
                    for (int j = 0; j < dim; j++)
                    {
                        result[mi, j] = MultipleOneElement(dim, mi, j, a, b, subProgress);
                    }
                });
            }

            foreach (var i in tasks) i.Wait();

            return result;
        }
        
        
        private object obj3 = new object();
        private double[,] CellsThreading(int dim, double[,] a, double[,] b, Action<int> progress)
        {
            int progressCount = 0;
            int count = 0;

            var result = new double[dim, dim];
            var tasks = new Task<double>[dim, dim];
            for (int i = 0; i < dim; i++)
            {

                for (int j = 0; j < dim; j++)
                {

                    int mi = i, mj = j;
                    tasks[i, j] = Task<double>.Run(() => MultipleOneElement(dim, mi, mj, a, b, () => {
                        lock (obj3)
                        {
                            count++;
                            if((count * 100) / dim > progressCount)
                                progress?.Invoke(++progressCount);
                        }
                    }));
                }
            }

            for (int i = 0; i < dim; i++)
            {
                for (int j = 0; j < dim; j++)
                {
                    tasks[i, j].Wait();
                    result[i, j] = tasks[i, j].Result;
                }
            }

            return result;
        }
    }
}