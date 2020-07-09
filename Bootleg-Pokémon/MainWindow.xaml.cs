﻿using GameConfig;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Bootleg_Pokémon
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        private readonly GameSession _gameSession = new GameSession();
        public MainWindow()
        {
            InitializeComponent();
            DataContext = _gameSession;
            _gameSession.IsGameCreated = false;
            _gameSession.Event += RaiseBattleMessages;

            using(StreamReader pokedexJson = new StreamReader("..\\..\\..\\pokedex.json"))
            {
                var json = pokedexJson.ReadToEnd();
                _gameSession.AllPokemon = JsonConvert.DeserializeObject<List<Pokemon>>(json);
                // MessageBox.Show(_gameSession.AllPokemon.Count().ToString());
            }
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            NewGame newGame = new NewGame();
            newGame.DataContext = _gameSession;
            newGame.Owner = this;
            newGame.Show();
        }
        
        private void Load_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFile = new OpenFileDialog();
            if (openFile.ShowDialog() == true)
            {
                string[] data = File.ReadAllLines(openFile.FileName);
                _gameSession.CurrentPlayer.Name = data[0].Substring(6);
                _gameSession.CurrentPlayer.Fights = int.Parse(data[1].Substring(data[1].IndexOf(':') + 2));
                _gameSession.CurrentPlayer.Wins = int.Parse(data[2].Substring(data[2].IndexOf(':') + 2));
                _gameSession.IsGameCreated = true;
                _gameSession.CurrentPlayer.Losses = _gameSession.CurrentPlayer.Fights - _gameSession.CurrentPlayer.Wins;

                if (_gameSession.CurrentPlayer.Fights != 0)
                {
                    _gameSession.CurrentPlayer.WinPercentage = Math.Round(Convert.ToDouble(_gameSession.CurrentPlayer.Wins) * 100.0 / Convert.ToDouble(_gameSession.CurrentPlayer.Fights), 2);
                }
                else { _gameSession.CurrentPlayer.WinPercentage = 0.0; }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFile = new SaveFileDialog();
            if (saveFile.ShowDialog() == true)
            {
                using(StreamWriter sw = File.CreateText(saveFile.FileName))
                {
                    sw.WriteLine($"Name: {_gameSession.CurrentPlayer.Name}");
                    sw.WriteLine($"Fights: {_gameSession.CurrentPlayer.Fights}");
                    sw.WriteLine($"Wins: {_gameSession.CurrentPlayer.Wins}");
                    sw.WriteLine($"Catches: {_gameSession.CurrentPlayer.PokemonCollection.Count}");
                }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Save_Click(sender, e);
            Close();
        }

        private void Pokedex_Click(object sender, RoutedEventArgs e)
        {
            Pokedex pokedex = new Pokedex();
            pokedex.DataContext = _gameSession;
            pokedex.Owner = this;
            pokedex.Show();
        }

        private void InitializeOpponent(int id, string category, int level)
        {
            _gameSession.EnemyPokemon = _gameSession.AllPokemon.First(p => p.Id == id);
            _gameSession.EnemyPokemon.CurLevel = level;
            _gameSession.EnemyPokemon.Category = category;
            _gameSession.GeneratePokemonStats(_gameSession.EnemyPokemon);
            EnemyCorner.Visibility = Visibility.Visible;
            MenuBar.IsEnabled = false;
        }

        private void Brock_Click(object sender, RoutedEventArgs e)
        {
            InitializeOpponent(74, "Trainer", 12);
            FightStatus.Document.Blocks.Add(new Paragraph(new Run($"Brock chose {_gameSession.EnemyPokemon.Name}")));
        }

        private void Misty_Click(object sender, RoutedEventArgs e)
        {
            InitializeOpponent(120, "Trainer", 18);
            FightStatus.Document.Blocks.Add(new Paragraph(new Run($"Misty chose {_gameSession.EnemyPokemon.Name}")));
        }

        private void Surge_Click(object sender, RoutedEventArgs e)
        {
            InitializeOpponent(26, "Trainer", 22);
            FightStatus.Document.Blocks.Add(new Paragraph(new Run($"Lt. Surge chose {_gameSession.EnemyPokemon.Name}")));
        }

        private void Erika_Click(object sender, RoutedEventArgs e)
        {
            InitializeOpponent(45, "Trainer", 25);
            FightStatus.Document.Blocks.Add(new Paragraph(new Run($"Erika chose {_gameSession.EnemyPokemon.Name}")));
        }

        private void Sabrina_Click(object sender, RoutedEventArgs e)
        {
            InitializeOpponent(49, "Trainer", 28);
            FightStatus.Document.Blocks.Add(new Paragraph(new Run($"Sabrina chose {_gameSession.EnemyPokemon.Name}")));
        }

        private void Pokemon_Choose_Click(object sender, RoutedEventArgs e)
        {
            if (PlayerPokemon.SelectedItem != null && _gameSession.CurrentPlayer.ChosenPokemon == null)
            {
                _gameSession.CurrentPlayer.ChosenPokemon = PlayerPokemon.SelectedItem as Pokemon;

                if (_gameSession.IsBattle)
                {
                    _gameSession.CurrentPlayer.ChosenPokemon.Moves = _gameSession.CurrentPlayer.PokemonCollection[PlayerPokemon.SelectedIndex].Moves;
                    PlayerCorner.Visibility = Visibility.Visible;
                    _gameSession.CurrentPlayer.Fights += 1;
                    FightStatus.Document.Blocks.Add(new Paragraph(new Run($"You chose {_gameSession.CurrentPlayer.ChosenPokemon.Name}")));
                }
                else
                {
                    MessageBox.Show($"{_gameSession.CurrentPlayer.ChosenPokemon.Name}: Level {_gameSession.CurrentPlayer.ChosenPokemon.CurLevel}");
                }
            }
        }

        public Move PlayerMoveSelected => _gameSession.CurrentPlayer.ChosenPokemon.Moves.FirstOrDefault(m => m.IsSelected);

        private void Move_Click(object sender, RoutedEventArgs e)
        {
            if (_gameSession.IsBattle && PlayerMoveSelected != null)
            {
                _gameSession.MoveOutcome(PlayerMoveSelected, _gameSession.CurrentPlayer.ChosenPokemon, _gameSession.EnemyPokemon);

                if (_gameSession.EnemyPokemon.CurHp <= 0)
                {
                    _gameSession.PlayerWon();

                    if (_gameSession.EnemyPokemon.Category.Equals("Trainer"))
                    {
                        if (_gameSession.EnemyPokemon.Id == 74 && !_gameSession.CurrentPlayer.BadgeCollection.Any(b => b.Equals("Brock")))
                        {
                            MessageBox.Show("You earned the BoulderBadge");
                            _gameSession.CurrentPlayer.BadgeCollection.Add("Brock");
                            Misty.IsEnabled = true;
                        }

                        if (_gameSession.EnemyPokemon.Id == 120 && !_gameSession.CurrentPlayer.BadgeCollection.Any(b => b.Equals("Misty")))
                        {
                            MessageBox.Show("You earned the CascadeBadge");
                            _gameSession.CurrentPlayer.BadgeCollection.Add("Misty");
                            Surge.IsEnabled = true;
                        }
                        if(_gameSession.EnemyPokemon.Id == 26 && !_gameSession.CurrentPlayer.BadgeCollection.Any(b => b.Equals("Surge")))
                        {
                            MessageBox.Show("You earned the ThunderBadge");
                            _gameSession.CurrentPlayer.BadgeCollection.Add("Surge");
                            Erika.IsEnabled = true;
                        }
                        if (_gameSession.EnemyPokemon.Id == 45 && !_gameSession.CurrentPlayer.BadgeCollection.Any(b => b.Equals("Erika")))
                        {
                            MessageBox.Show("You earned the RainbowBadge");
                            _gameSession.CurrentPlayer.BadgeCollection.Add("Erika");
                            Sabrina.IsEnabled = true;
                        }
                        if (_gameSession.EnemyPokemon.Id == 49 && !_gameSession.CurrentPlayer.BadgeCollection.Any(b => b.Equals("Sabrina")))
                        {
                            MessageBox.Show("You earned the RainbowBadge");
                            _gameSession.CurrentPlayer.BadgeCollection.Add("Sabrina");
                            Sabrina.IsEnabled = true;
                        }
                    }

                    EndFight.Visibility = Visibility.Visible;
                    return;
                }

                Random rnd = new Random();
                _gameSession.MoveOutcome(_gameSession.EnemyPokemon.Moves[rnd.Next(0, 4)], _gameSession.EnemyPokemon, _gameSession.CurrentPlayer.ChosenPokemon);

                if (_gameSession.CurrentPlayer.ChosenPokemon.CurHp <= 0)
                {
                    _gameSession.OpponentWon();
                    EndFight.Visibility = Visibility.Visible;
                    return;
                }
            }
        }

        private void End_Fight_Click(object sender, RoutedEventArgs e)
        {
            _gameSession.EnemyPokemon = null;

            GenFunctions.BattleStatsGenerator(_gameSession.CurrentPlayer.PokemonCollection.First(p => p.Id == _gameSession.CurrentPlayer.ChosenPokemon.Id));
            GenFunctions.PokemonLeveller(_gameSession.CurrentPlayer.PokemonCollection.First(p => p.Id == _gameSession.CurrentPlayer.ChosenPokemon.Id));

            _gameSession.CurrentPlayer.ChosenPokemon = null;

            PlayerCorner.Visibility = Visibility.Hidden;
            EnemyCorner.Visibility = Visibility.Hidden;
            EndFight.Visibility = Visibility.Hidden;
            MenuBar.IsEnabled = true;
            FightStatus.Document.Blocks.Clear();
        }

        private void RaiseBattleMessages(object sender, GameMessageEventArgs e)
        {
            FightStatus.Document.Blocks.Add(new Paragraph(new Run(e.Message)));
            FightStatus.ScrollToEnd();
        }
    }
}
