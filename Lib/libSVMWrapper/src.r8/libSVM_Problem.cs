/*
 * GNU LESSER GENERAL PUBLIC LICENSE
 * Version 3, 29 June 2007
 * 
 * Copyright 2011 Rafał "R@V" Prasał <rafal.prasal@gmail.com>
*/

using System;
using System.IO;
using System.Globalization;


/// <summary>
/// Package for wrapper for libsvm.dll
/// </summary>
namespace libSVMWrapper
{
    /// <summary>
    /// Problem for training
    /// </summary>
    /// Usage Examples:
    /// 
    ///     Loading problem:
    ///     
    /// @code
    ///             libSVM_Problem problem = new libSVM_Problem();
    ///             problem.Reload("file name");        
    /// @endcode
    ///             
    ///         or
    ///         
    /// @code
    ///             libSVM problem = libSVM_Problem.Load("file name");
    /// @endcode
    ///             
    ///     Saving problem:
    ///     
    /// @code
    ///             libSVM problem;
    ///             
    ///             problem.Save("file name");
    /// @endcode
    ///     
    public class libSVM_Problem
    {
        /// <summary>
        /// label correspondig to sample
        /// </summary>
        public double[] labels = null;

        /// <summary>
        /// samples
        /// </summary>
        public double[][] samples = null;

        /// <summary>
        /// Save problem to a file in LibSVM format
        /// </summary>
        /// <param name="_filename"></param>
        public void Save(string _filename)
        {
            if(labels==null || labels.Length==0) throw new Exception("no labels");
            if(samples==null || samples.Length==0) throw new Exception("no samples");
            if(labels.Length!=samples.Length) throw new Exception("labels.length != samples.length");
            
            StreamWriter sw = new StreamWriter(File.Create(_filename));

            for (int i = 0; i < labels.Length; i++)
            {
                sw.Write(labels[i].ToString(CultureInfo.InvariantCulture));
                sw.Write(" ");

                if (samples[i] == null) throw new Exception("samples[ " + i + " ] = null");

                for (int j = 0; j < samples[i].Length; j++)
                    sw.Write(j +":" + samples[i][j].ToString(CultureInfo.InvariantCulture) + " ");

                sw.WriteLine("");
            }

            sw.Close();
        }

        /// <summary>
        /// Reloading Problem from a libSVM format file
        /// </summary>
        /// <param name="_filename">problem file</param>
        public void Reload(string _filename)
        {
            StreamReader sr = new StreamReader(File.Open(_filename, FileMode.Open));

            int nr_samples = 0;
            int sample_len = 0;

            samples = null;
            labels = null;

            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();

                if (samples == null)
                {
                    labels = new double[1];
                    samples = new double[1][];
                    nr_samples = 1;
                }
                else
                {
                    nr_samples++;
                    Array.Resize(ref samples, nr_samples);
                    Array.Resize(ref labels, nr_samples);
                }

                string[] comment = line.Split('#');
                string line_values = comment[0];
                string[] values = line_values.Split(" \t".ToCharArray());

                if (values.Length == 0) throw new Exception("no values in line");

                double[] sample = null;

                for (int i = values.Length - 1; i > 0; i--)
                {
                    string[] index_value = null;

                    if (values[i] != "")
                    {
                        index_value = values[i].Split(':');

                        if (index_value.Length != 2)
                            throw new Exception("value in line is not index:value");
                    }

                    int index = int.Parse(index_value[0], CultureInfo.InvariantCulture);
                    double value = double.Parse(index_value[1], CultureInfo.InvariantCulture);

                    if (index < 0) throw new Exception("index:value -> index<0");

                    if (sample == null)
                    {
                        sample_len = index;
                        sample = new double[sample_len];

                        for (int j = 0; j < sample_len; j++)
                            sample[j] = 0.0;
                    }
                    else
                        if (index > sample_len)
                        {
                            for (int k = 0; k < nr_samples; k++)
                            {
                                Array.Resize(ref sample, index);

                                for (int j = sample_len; j < index; j++)
                                    sample[j] = 0.0;
                            }

                            sample_len = index;
                        }

                    sample[index - 1] = value;
                }

                labels[nr_samples - 1] = double.Parse(values[0], CultureInfo.InvariantCulture);
                samples[nr_samples - 1] = sample;
            }
        }

        /// <summary>
        /// Load problem from a file in libSVM format
        /// </summary>
        /// <param name="_filename">name of file</param>
        public static libSVM_Problem Load(string _filename)
        {
            libSVM_Problem problem = new libSVM_Problem();

            problem.Reload(_filename);

            return problem;
        }
    }
}
