﻿using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics;

namespace TspTest.Tsp
{
    public class Hopfield
    {
        private Matrix<double> _distances;
        private Func<int, int, int, int> to1D = (row, column, width) => row * width + column;
        private Func<int, int, Tuple<int, int>> to2D = (index, width) => new Tuple<int, int>(index % width, index / width);
        private Func<Matrix<double>, int, int, double> checkIndex = (Matrix<double> vv, int u, int v) => u >= 0 && u < vv.RowCount && v >= 0 && v < vv.ColumnCount ? vv[u, v] : 0;
        private double A, B, C, D, o, alpha;
        private int numberOfCities;
        public Hopfield(int numberOfCities, Matrix<double> distances, double A = 100, double B = 100, double C = 90, double D = 100, double o = 1.1, double alpha = 50)
        {
            this._distances = distances;
            var t = numberOfCities * numberOfCities;
            this.numberOfCities = numberOfCities;
            this.A = A;
            this.B = B;
            this.C = C;
            this.D = D;
            this.o = o;
            this.alpha = alpha;
        }
        
        public IList<int> Solve()
        {
            Console.WriteLine();
            Vector<double> currentInput = Vector<double>.Build.Random(numberOfCities * numberOfCities, new MathNet.Numerics.Distributions.Beta(2, 2));
            Matrix<double> X = Matrix<double>.Build.Dense(numberOfCities, numberOfCities, 0);
            for (int i = 0; i < X.RowCount; i++)
            {
                X[i, i] = 1;
            }
            double prevEnergy = 0, energy = 1;
            int iterations = 1;
            IEnumerable<int> indices = Enumerable.Range(0, numberOfCities - 1);
            Random rnd = new Random();
            IEnumerable<int> firstIndexArray = indices.OrderBy(x => rnd.Next()),
                             secondIndexArray = indices.OrderBy(x => rnd.Next());
            while (prevEnergy != energy && iterations++ <= 1000)
            {
                prevEnergy = energy;
                foreach(var i in firstIndexArray)
                {
                    foreach(var j in secondIndexArray)
                    {
                        Func<double> f = () =>
                        {
                            //double temp1 = 0, temp2 = 0;
                            //for (int k = 0; k < numberOfCities; k++)
                            //{
                            //    temp1 += X[i, k];
                            //    temp2 += _distances[i, k] * (checkIndex(X, k, j + 1) + checkIndex(X, k, j - 1));
                            //}
                            //return -currentInput[to1D(i, j, numberOfCities)] / t - (A + B) * temp1 - C * (X.RowSums().Sum() - m) - D * temp2;
                            return - A * X.Row(i).EnumerateIndexed().Where(xx => xx.Item1 != j).Select(xx => xx.Item2).Sum()
                                   - B * X.Column(j).EnumerateIndexed().Where(xx => xx.Item1 != i).Select(xx => xx.Item2).Sum()
                                   - C * (X.RowSums().Sum() - (numberOfCities + o))
                                   - D * _distances.Row(i).EnumerateIndexed().Select(xx => xx.Item2 * (checkIndex(X, xx.Item1, j + 1) + checkIndex(X, xx.Item1, j - 1))).Sum();
                        };
                        var index = to1D(i, j, numberOfCities);
                        currentInput[index] = f();
                        X[i, j] = (1 + Math.Tanh(this.alpha * currentInput[index])) / 2;
                    }
                }
                energy = _energy(X);
                Console.WriteLine("Energy = " + energy);
            }
            Console.WriteLine(X);
            X = X.Map(e => Math.Round(e));
            IList<int> path = Enumerable.Repeat(0, numberOfCities + 1).ToList();
            //X.EnumerateRowsIndexed().ToList().ForEach(x => { path[x.Item2.EnumerateIndexed().Where(xx => xx.Item2 == 1).Select(xx => xx.Item1).First()] = x.Item1 + 1; });
            for (int i = 0; i < X.RowCount; i++)
            {
                for (int j = 0; j < X.ColumnCount; j++)
                {
                    if (X[i, j] == 1)
                        path[j + 1] = i + 1;
                }
            }
            return path;
        }

        private double _energy(Matrix<double> v)
        {
            double temp1 = 0, temp2 = 0, temp3 = 0;
            for (int x = 0; x < numberOfCities; x++)
            {
                for (int i = 0; i < numberOfCities; i++)
                {
                    for (int j = 0; j < numberOfCities; j++)
                    {
                        if(i != j)
                        {
                            temp1 += v[x, i] * v[x, j];
                            temp2 += v[i, x] * v[j, x];
                        }
                        if(x != i)
                        {
                            temp3 += _distances[x, i] * v[i, j] * (checkIndex(v, i, j - 1) + checkIndex(v, i, j + 1));
                        }
                    }
                }
            }
            return A * temp1 / 2 + B * temp2 / 2 + D * temp3 / 2 + C * Math.Pow(v.RowSums().Sum() - (numberOfCities + o), 2) / 2;
        }
    }
}