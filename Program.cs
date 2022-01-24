using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System;

namespace Reversi {
  public enum CellState {
    Empty, Player1, Player2, PossibleMove
  }

  public enum Screen {
    Main, Game, GameOver
  }

  class Move {
    public Point position;
    public List<Point> changedCells = new List<Point>();

    public Move() { }
  };

  public class GameBoard : Panel {
    int Cols = 4;
    int Rows = 4;
    int OffsetX, OffsetY;

    double dd;

    public bool HelpEnabled = false;
    public bool Speler1NoMoves = false;
    public bool Speler2NoMoves = false;

    public event EventHandler OnWin;
    public event EventHandler OnUpdate;

    CellState[,] Board = new CellState[8, 8];
    public CellState CurrentPlayer = CellState.Player1;

    List<Move> moves = new List<Move>();

    public GameBoard() {
      this.Size = new Size(Width + 1, Height + 1);
      this.Location = new Point(50, 125);
      this.BackColor = Color.White;
      this.Paint += this.OnPaint;
      this.MouseClick += this.OnClick; ;
      this.Height = 501;
      this.Width = 501;
    }

    private int countStones(CellState player) {
      int Count = 0;
      for (int y = 0; y < Rows; y++) {
        for (int x = 0; x < Cols; x++) {
          if (this.Board[y, x] == player) {
            Count++;
          }
        }
      }

      return Count;
    }

    public int Player1Count {
      get { return countStones(CellState.Player1); }
    }

    public int Player2Count {
      get { return countStones(CellState.Player2); }
    }

    public void NewGame(int Columns, int Rows) {
      this.Cols = Columns;
      this.Rows = Rows;
      this.Board = new CellState[Rows, Columns];
      this.HelpEnabled = false;

      for (int y = 0; y < this.Rows; y++) {
        for (int x = 0; x < this.Cols; x++) {
          this.Board[y, x] = CellState.Empty;
        }
      }

      this.Board[(this.Rows - 2) / 2, (this.Cols - 2) / 2] = CellState.Player2;
      this.Board[(this.Rows - 2) / 2, (this.Cols - 2) / 2 + 1] = CellState.Player1;
      this.Board[(this.Rows - 2) / 2 + 1, (this.Cols - 2) / 2] = CellState.Player1;
      this.Board[(this.Rows - 2) / 2 + 1, (this.Cols - 2) / 2 + 1] = CellState.Player2;

      this.CurrentPlayer = CellState.Player1;
      this.GeneratePossibleMoves();
      this.Invalidate();
    }

    private void OnClick(object _, MouseEventArgs e) {
      if (
        e.Y > this.OffsetY && e.Y < this.OffsetY + this.dd * this.Rows
        && e.X > this.OffsetX && e.X < this.OffsetX + this.dd * this.Cols
      ) {
        int col = (int)((e.X - this.OffsetX) / (dd * this.Cols) * this.Cols);
        int row = (int)((e.Y - this.OffsetY) / (dd * this.Rows) * this.Rows);

        if (this.Board[row, col] == CellState.PossibleMove) {
          this.Board[row, col] = this.CurrentPlayer;

          foreach (var move in this.moves) {
            if (move.position == new Point(col, row)) {
              foreach (var cell in move.changedCells)
                this.Board[cell.Y, cell.X] = this.CurrentPlayer;
            }
          }

          this.CurrentPlayer = this.OtherPlayer;
          this.ResetPossibleMoves();
          this.GeneratePossibleMoves();

          this.OnUpdate?.Invoke(this, null);

          if (this.Player1Count + this.Player2Count == this.Rows * this.Cols)
            this.OnWin?.Invoke(this, null);

          this.Invalidate();
        }
      }
    }

    public CellState OtherPlayer {
      get { return (this.CurrentPlayer == CellState.Player1) ? CellState.Player2 : CellState.Player1; }
    }

    private bool isValidCell(int x, int y) {
      return x >= 0 && x < this.Cols && y >= 0 && y < this.Rows;
    }

    private void ResetPossibleMoves() {
      for (int y = 0; y < this.Rows; y++)
        for (int x = 0; x < this.Cols; x++)
          if (this.Board[y, x] == CellState.PossibleMove) this.Board[y, x] = CellState.Empty;

      this.moves.Clear();
    }

    readonly Point[] directions = { new Point(0, -1), new Point(1, -1), new Point(1, 0), new Point(1, 1), new Point(0, 1), new Point(-1, 1), new Point(-1, 0), new Point(-1, -1) };
    public void GeneratePossibleMoves() {
      for (int Row = 0; Row < this.Rows; Row++) {
        for (int Col = 0; Col < this.Cols; Col++) {
          if (this.Board[Row, Col] == this.CurrentPlayer) {
            for (int Direction = 0; Direction < directions.Length; Direction++) {
              int x = Col, y = Row;
              var move = new Move();

              while (this.isValidCell(x + this.directions[Direction].X, y + this.directions[Direction].Y) && this.Board[y + this.directions[Direction].Y, x + this.directions[Direction].X] == this.OtherPlayer) {
                x += this.directions[Direction].X;
                y += this.directions[Direction].Y;

                move.changedCells.Add(new Point(x, y));
              }

              int newY = y + this.directions[Direction].Y;
              int newX = x + this.directions[Direction].X;
              if (this.isValidCell(newX, newY) && !(y == Row && x == Col) && (this.Board[newY, newX] == CellState.Empty || this.Board[newY, newX] == CellState.PossibleMove)) {
                this.Board[newY, newX] = CellState.PossibleMove;
                move.position = new Point(newX, newY);
                this.moves.Add(move);
              }
            }
          } else if (this.Board[Row, Col] != this.OtherPlayer && this.Board[Row, Col] != CellState.PossibleMove) {
            this.Board[Row, Col] = CellState.Empty;
          }
        }
      }

      if (this.moves.Count == 0) {
        if (this.CurrentPlayer == CellState.Player1)
          this.Speler1NoMoves = true;
        else
          this.Speler2NoMoves = true;

        this.CurrentPlayer = this.OtherPlayer;

        if (this.Speler1NoMoves && this.Speler2NoMoves) {
          this.OnUpdate?.Invoke(this, null);
          this.OnWin?.Invoke(this, null);
        } else {
          this.GeneratePossibleMoves();
        }
      } else {
        this.Speler1NoMoves = false;
        this.Speler2NoMoves = false;
      }
    }

    public void OnPaint(object _, PaintEventArgs pea) {
      this.dd = (double)500 / Math.Max(this.Rows, this.Cols);

      this.OffsetX = 0;
      if (this.Rows > this.Cols) this.OffsetX = (int)((this.Width - this.Cols * this.dd) / 2);

      this.OffsetY = 0;
      if (this.Rows < this.Cols) this.OffsetY = (int)((this.Height - this.Rows * this.dd) / 2);

      Pen p = new Pen(Brushes.Black);

      for (int i = 0; i <= this.Cols; i++) {
        double x = this.dd * i;
        pea.Graphics.DrawLine(p, new Point((int)(x + this.OffsetX), this.OffsetY), new Point((int)x + this.OffsetX, (int)(this.OffsetY + this.dd * this.Rows)));
      }

      for (int i = 0; i <= this.Rows; i++) {
        double y = this.dd * i;
        pea.Graphics.DrawLine(p, new Point(this.OffsetX, (int)y + this.OffsetY), new Point((int)(this.OffsetX + this.dd * this.Cols), (int)y + this.OffsetY));
      }

      for (int Row = 0; Row < this.Rows; Row++) {
        for (int Col = 0; Col < this.Cols; Col++) {
          double x = this.dd * Col;

          if (this.Rows > this.Cols) x += (this.Width - this.Cols * this.dd) / 2;
          double y = dd * Row;

          if (this.Rows < this.Cols) y += (this.Height - this.Rows * this.dd) / 2;

          if (this.Board[Row, Col] == CellState.Player1) {
            pea.Graphics.FillEllipse(Brushes.Blue, (int)(.05 * this.dd + x), (int)(.05 * this.dd + y), (int)(.9 * this.dd), (int)(.9 * this.dd));
          } else if (this.Board[Row, Col] == CellState.Player2) {
            pea.Graphics.FillEllipse(Brushes.Red, (int)(.05 * this.dd + x), (int)(.05 * this.dd + y), (int)(.9 * this.dd), (int)(.9 * this.dd));
          } else if (this.Board[Row, Col] == CellState.PossibleMove && HelpEnabled) {
            if (this.CurrentPlayer == CellState.Player2) {
              Brush r = new SolidBrush(Color.FromArgb(75, 255, 0, 0));
              pea.Graphics.FillEllipse(r, (int)(.05 * this.dd + x), (int)(.05 * this.dd + y), (int)(.9*this.dd), (int)(.9*this.dd));
            } else {
              Brush b = new SolidBrush(Color.FromArgb(100, 0, 0, 255));
              pea.Graphics.FillEllipse(b, (int)(.05 * this.dd +  x), (int)(.05 * this.dd + y), (int)(.9*this.dd), (int)(.9*this.dd));
            }
          }
        }
      }
    }
  }

  public class Game : Form {
    GameBoard Board;
    Label l, P1, P2, Result, BlueStones, RedStones;
    TextBox Columns, Rows;

    Control[,] Screens = new Control[3, 10];
    Screen CurrentScreen = Screen.Main;

    public Game() {
      this.Size = new Size(700, 700);
      this.Paint += OnPaint;
      this.BackColor = Color.White;
      this.Text = "Reversi";

      this.BuildMainScreen();
      this.BuildGameScreen();
      this.BuildGameOverScreen();
      this.UpdateScreen();
    }

    public void UpdateScreen() {
      for (int i = 0; i < 3; i++)
        for (int j = 0; j < 10; j++)
          if (this.Screens[i, j] != null)
            this.Screens[i, j].Visible = (i == (int)this.CurrentScreen);

      this.Invalidate();
    }

    public Label BuildLabel(int x, int y, int width, int height, int fontSize, string text, Screen s, int index) {
      var l = new Label {
        Size = new Size(width, height),
        Font = new Font(SystemFonts.DefaultFont.FontFamily, fontSize),
        Location = new Point(x, y),
        Text = text
      };
      this.Screens[(int)s, index] = l;
      this.Controls.Add(l);
      return l;
    }

    private Button BuildButton(int x, int y, int width, int height, string text, EventHandler handler, Screen s, int index) {
      var b = new Button {
        Size = new Size(width, height),
        Location = new Point(x, y),
        Text = text
      };
      b.Click += handler;
      this.Screens[(int)s, index] = b;
      this.Controls.Add(b);
      return b;
    }

    private TextBox BuildTextbox(int x, int y, int width, int height, string text , Screen s, int index) {
      var t = new TextBox {
        Size = new Size(width, height),
        Location = new Point(x, y),
        Text = text
      };
      this.Screens[(int)s, index] = t;
      this.Controls.Add(t);
      return t;
    }

    public void BuildMainScreen() {
      BuildLabel(330, 200, 300, 50, 25, "Reversi", Screen.Main, 0);

      this.Columns = BuildTextbox(200, 350, 100, 100, "Columns", Screen.Main, 1);
      this.Rows = BuildTextbox(400, 350, 100, 100, "Rows", Screen.Main, 2);

      BuildButton(300, 500, 100, 30, "Start", OnStart, Screen.Main, 3);
    }

    public void BuildGameScreen() {
      this.Board = new GameBoard();
      this.Board.OnUpdate += OnMoveUpdate;
      this.Board.OnWin += OnWin;
      this.Screens[(int)Screen.Game, 0] = Board;
      this.Controls.Add(Board);

      BuildButton(350, 20, 100, 30, "Reset", OnResetGame, Screen.Game, 1);
      BuildButton(350, 60, 100, 30, "Help", OnHelpClicked, Screen.Game, 2);
      var b = BuildButton(470, 20, 100, 30, "Give up", OnGiveUp, Screen.Game, 3);
      b.Visible = false;

      this.l = BuildLabel(200, 50, 150, 100, 12, "Blue's move", Screen.Game, 4);

      this.P1 = BuildLabel(100, 31, 100, 50, 12, "2 Stones", Screen.Game, 5);
      this.P2 = BuildLabel(100, 81, 100, 50, 12, "2 Stones", Screen.Game, 6);
    }

    private void BuildGameOverScreen() {
      this.Result = BuildLabel(200, 100, 400, 200, 40, "", Screen.GameOver, 0);

      this.BlueStones = BuildLabel(270, 312, 150, 50, 20, "", Screen.GameOver, 1);
      BlueStones.ForeColor = Color.Blue;

      this.RedStones = BuildLabel(270, 392, 150, 50, 20, "", Screen.GameOver, 2);
      RedStones.ForeColor = Color.Red;

      BuildButton(300, 500, 100, 50, "Start new game", OnRestart, Screen.GameOver, 3);
    }

    private void OnPaint(object sender, PaintEventArgs e) {
      if (this.CurrentScreen == Screen.Game) {
        e.Graphics.FillEllipse(Brushes.Blue, 50, 20, 40, 40);
        e.Graphics.FillEllipse(Brushes.Red, 50, 70, 40, 40);
      } else if (this.CurrentScreen == Screen.GameOver) {
        e.Graphics.FillEllipse(Brushes.Blue, 200, 300, 60, 60);
        e.Graphics.FillEllipse(Brushes.Red, 200, 380, 60, 60);
      } else if (this.CurrentScreen == Screen.Main) {
        e.Graphics.FillEllipse(Brushes.Blue, 230, 175, 40, 40);
        e.Graphics.FillEllipse(Brushes.Blue, 270, 215, 40, 40);
        e.Graphics.FillEllipse(Brushes.Red, 270, 175, 40, 40);
        e.Graphics.FillEllipse(Brushes.Red, 230, 215, 40, 40);
      }
    }

    private void OnHelpClicked(object _, EventArgs __) {
      this.Board.HelpEnabled = !this.Board.HelpEnabled;
      this.Board.Invalidate();
    }

    private void OnResetGame(object _, EventArgs __) {
      this.Board.NewGame(int.Parse(this.Columns.Text), int.Parse(this.Rows.Text));
      this.OnMoveUpdate(this, null);
    }

    private void OnMoveUpdate(object _, EventArgs __) {
      if (!this.l.Text.EndsWith("!")) {
        if (Board.CurrentPlayer == CellState.Player1)
          this.l.Text = "Blue's move";
        else
          this.l.Text = "Red's move";

        this.P1.Text = this.Board.Player1Count.ToString() + ((this.Board.Player1Count == 1) ? " Stone" : " Stones");
        this.P2.Text = this.Board.Player2Count.ToString() + ((this.Board.Player2Count == 1) ? " Stone" : " Stones");
        this.Invalidate();
      }
    }

    private void OnWin(object _, EventArgs __) {
      if (this.Board.Player1Count > this.Board.Player2Count) {
        this.Result.Text = "Blue is the winner!";
        this.Result.ForeColor = Color.Blue;
      } else if (this.Board.Player1Count == this.Board.Player2Count) {
        this.Result.Text = "Tie!";
      } else {
        this.Result.Text = "Red is the winner";
        this.Result.ForeColor = Color.Red;
      }

      this.CurrentScreen = Screen.GameOver;
      this.BlueStones.Text = this.P1.Text;
      this.RedStones.Text = this.P2.Text;

      this.UpdateScreen();
    }

    public void OnGiveUp(object _, EventArgs __) {
      this.CurrentScreen = Screen.GameOver;
      this.Result.Text = (this.Board.CurrentPlayer == CellState.Player1)
         ? "Red is the winner!"
         : "Blue is the winner!";

      this.Result.ForeColor = (this.Board.CurrentPlayer == CellState.Player1)
          ? Color.Red
          : Color.Blue;

      this.BlueStones.Text = P1.Text;
      this.RedStones.Text = P2.Text;
      this.UpdateScreen();
    }

    public void OnStart(object _, EventArgs __) {
      try {
        this.Board.NewGame(int.Parse(this.Columns.Text), int.Parse(this.Rows.Text));
        this.CurrentScreen = Screen.Game;
        this.UpdateScreen();
        this.OnMoveUpdate(this, null);
      } catch (Exception) {
        MessageBox.Show("Enter a valid value, please.");
      }
    }

    public void OnRestart(object _, EventArgs __) {
      this.CurrentScreen = Screen.Main;
      this.UpdateScreen();
    }

    public static void Main() {
      Application.Run(new Game());
    }
  }
}
