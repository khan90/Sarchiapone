﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ProgettoMMDS
{
    class GreedyAntsScheduler : AbstractScheduler
    {
        Schedule schedule;
        /// <summary>
        /// Matrice con la traccia di ferormone
        /// Il primo indice (righe) rappresenta il job
        /// Il secondo indice rappresenta lo stadio
        /// traceMatrix[j,i] = job J preso in posizione i
        /// </summary>
        double[,] traceMatrix;

        public override void run(string[] args)
        {
            if (!(args.Length > 0))
            {
                Console.WriteLine("PATH non specificato");
                Console.ReadKey();
                return;
            }
            else
            {
                FileManager fm = new FileManager(args[0]);
                jobs = (fm.getJobsList());
                //INIZIO CONTEGGIO SECONDI
                DateTime startTime = DateTime.Now;
                //QUI l'algoritmo di ricerca del minimo ->
                Schedule schedule = greedyAnts(fm.getNumberofMachine(), fm.getNumberofJobs());

                //FINE CONTEGGIO SECONDI
                DateTime stopTime = DateTime.Now;
                TimeSpan elapsedTime = stopTime.Subtract(startTime);
                //Stampa Tardiness e tempo totale
                Console.WriteLine("Tardiness totale: " + schedule.getTardiness().ToString());
                Console.WriteLine("Arrivato in " + elapsedTime.TotalMilliseconds + " ms");

                //OUTPUT
                fm.OutputSolution(schedule.schedule);
                fm.OutputResult(schedule.getTardiness(), elapsedTime.TotalMilliseconds);
                fm.OutputProva(schedule.schedule, schedule.getTardiness(), elapsedTime.TotalMilliseconds, "Prova");
                Console.ReadKey();
            }
        }

        private Schedule greedyAnts(int m, int n)
        {
            Thread thread = new Thread(new ThreadStart(timer));
            thread.Start();
            schedule = new Schedule(jobs);
            schedule.constructScheduleEDD(m, n);
            //Inizializzazione matrice delle traccie
            traceMatrix = new double[schedule.Count(), schedule.Count()];
            for (int i = 0; i < schedule.Count(); i++)
            {                
                for (int j = 0; j < schedule.Count(); j++)
                {
                    traceMatrix[i, j] = 50;
                }
            }
            int iteration = 0;
            while (!fine)
            {
                iteration++;
                constructGreedysolutions(iteration);
            }
            
            for (int i = 0; i < schedule.Count(); i++)
            {
                for (int j = 0; j < schedule.Count(); j++)
                {
                    Console.Write((int)traceMatrix[i, j] + " ");
                }
                Console.Write("\n");
            }
            return schedule;
        }

        private void constructGreedysolutions(int iteration)
        {
            //NUMERO DI FORMICHE
            int ants = 10;
            double alfa = 0.7;
            int bestTardiness = schedule.getTardiness();
            Stack<Schedule> solutionList = new Stack<Schedule>();
            //faccio partire le formiche
            for (int j = 0; j < ants; j++)
            {
                solutionList.Push(antRun(iteration));
            }
            
            //EVAPORAZIONE
            for (int i = 0; i < schedule.Count(); i++)
            {
                for (int j = 0; j < schedule.Count(); j++)
                {
                    traceMatrix[i, j] *= alfa;
                }
            }
            //Console.WriteLine(solutionList.Count());
            while (solutionList.Count != 0)
            {
                Schedule currentSchedule = solutionList.Pop();
                int currentTardiness = currentSchedule.getTardiness();

                if (currentTardiness < bestTardiness)
                {
                    schedule = new Schedule(currentSchedule);
                    bestTardiness = currentTardiness;                    
                }
                //Se la tardiness è zero ritorno, ho trovato l'ottimo.
                if (0 == currentTardiness)
                {
                    fine = true;
                    return;                    
                }
                //AGGIORNAMENTO TRACCIA
                //Console.WriteLine(bestTardiness +" "+ currentTardiness);
                for (int i = 0; i < currentSchedule.Count(); i++)
                {
                    int elemento = currentSchedule.schedule[i];                   
                    traceMatrix[elemento, i] += (traceMatrix[elemento,i] * bestTardiness/currentTardiness);                    
                    if (traceMatrix[elemento, i] > 100)
                    {
                        traceMatrix[elemento, i] = 100;
                    }
                }
            }    
        }

        private Schedule antRun(int iteration)
        {
            iteration /= 2;
            Random r = new Random();
            int choice = r.Next(100);
            int soglia = 70;
            //Lista dei job candidati da inserire nello schedule
            Schedule candidateList = new Schedule(schedule);
            //Schedule parziale
            Schedule partialSchedule = new Schedule(jobs);
            for (int i = 0; i < schedule.Count(); i++)
            {
                if (choice < soglia)
                {
                    //SFRUTTAMENTO -> Seguo il ferormone e prendo quello che ha il valore più alto
                    double maxValue = -1.0;
                    int bestJob = 0;
                    for (int j = 0; j < candidateList.Count(); j++)
                    {
                        if (traceMatrix[j, i] > maxValue)
                        {
                            bestJob = j;
                            maxValue = traceMatrix[j, i];
                        }
                    }
                    partialSchedule.Add(candidateList.schedule[bestJob]);
                    candidateList.schedule.RemoveAt(bestJob);
                }
                else
                {
                    //ESPLORAZIONE -> Idea: Effettuo una GRASP
                    int maxIndex = iteration;
                    if (iteration >= candidateList.Count()){
                        maxIndex = candidateList.Count() - 1;
                    }
                    int index = r.Next(maxIndex);
                    partialSchedule.Add(candidateList.schedule[index]);
                    candidateList.schedule.RemoveAt(index);
                }
            }
            return LocalSearchBestInsert(LocalSearchBestInsert(partialSchedule));
        }
    }
}
