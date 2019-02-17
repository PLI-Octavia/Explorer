using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class MusicNotes : MonoBehaviour
{
    private System.Random _rnd = new System.Random();
    private GameObject[] _musicNotes;
    private IEnumerable<string> _colorNames;
    private Dictionary<string, GameObject> _notesForColor;
    private Dictionary<string, Vector3> _initialColorPos;
    private List<string> _sequence;

    // Start is called before the first frame update
    void Start()
    {
        _musicNotes = GameObject.FindGameObjectsWithTag("MusicNote");
        if (_musicNotes.Length < 2)
        {
            Debug.Log("Not enough music notes, needs at least 2.");
            return;
        }

        _colorNames = _musicNotes.Select(GetNoteColor);

        _notesForColor = _musicNotes.ToDictionary(GetNoteColor, x => x);
        _initialColorPos = _musicNotes.ToDictionary(GetNoteColor, x => x.transform.position); /* no need to clone the pos */
        var numInSequence = 3; /* TODO config here */
        _sequence = GenerateSequence(numInSequence);

        StartGameMode(GameState.SHOW);
    }

    private void StartGameMode(GameState s)
    {
        _curSequence = 0; /* start at object 0 */
        StopVibrating();
        Debug.Log("Start mode: " + s.ToString());
        _gameState = s;
    }

    private void SetGameState(GameState s)
    {
        Debug.Log("Next state: " + s.ToString());
        _gameState = s;
    }

    private string GetNoteColor(GameObject note)
    {
        return note.name.Substring("MusicNote".Length);
    }

    private List<string> GenerateSequence(int num)
    {
        Debug.Log("Generating sequence...");
        string DrawColor()
        {
            return _colorNames.OrderBy(_ => _rnd.Next()).First();
        }
        return Enumerable.Range(0, num).Select(_ => DrawColor()).ToList();
    }
    
    enum GameState { IDLE, WAIT_SHOW, SHOW, WAIT_PLAY, PLAY, LOSS, WIN };
    private GameState _gameState = GameState.IDLE;
    private int _curSequence = 0;
    private string _curVibrating = null;
    void Update()
    {
        if (_curVibrating != null)
        {
            Vibrate(_curVibrating);
        }

        ++_curFrame;
        if (!EveryFrame(3 * 60, true))
        {
            return;
        }

        switch (_gameState)
        {
            case GameState.IDLE:
                break;

            case GameState.WAIT_SHOW:
                UpdateWaitShow();
                break;

            case GameState.SHOW:
                UpdateShow();
                break;

            case GameState.WAIT_PLAY:
                UpdateWaitPlay();
                break;

            case GameState.PLAY:
                UpdatePlay();
                break;

            case GameState.LOSS:
                break;

            case GameState.WIN:
                break;

            default:
                Debug.Log("invalid game state! this is a bug.");
                StartGameMode(GameState.IDLE);
                break;
        }
    }

    private int _curFrame = 0;
    private bool EveryFrame(int v, bool reset = false) /* do we need reset? */
    {
        var isCorrect = (_curFrame == 0 || (_curFrame % v) == 0); /* every v frames */
        if (isCorrect && reset)
        {
            _curFrame = 0;
        }
        return isCorrect;
    }

    private void Vibrate(string color)
    {
        if (!EveryFrame(4))
        {
            return;
        }

        var obj = _notesForColor[color];
        var initial = _initialColorPos[color];

        float diffX = _rnd.Next(10) / 20f;
        float diffZ = _rnd.Next(10) / 20f;
        if (_rnd.Next(1) == 1)
            diffX *= -1; /* go the other way */
        if (_rnd.Next(1) == 1)
            diffZ *= -1; /* go the other way */
        obj.transform.position = new Vector3(initial.x + diffX, initial.y, initial.z + diffZ);
    }

    private void StopVibrating()
    {
        if (_curVibrating == null)
        {
            return;
        }

        var obj = _notesForColor[_curVibrating];
        obj.transform.position = _initialColorPos[_curVibrating];
        _curVibrating = null; /* stop vibrating this object */
    }

    private void UpdateWaitShow()
    {
        StopVibrating();

        if (_curSequence == _sequence.Count() - 1)
        { /* stop */
            StartGameMode(GameState.PLAY);
        }
        else
        {
            /* next tick, will use next color */
            _curSequence++;
            SetGameState(GameState.SHOW);
        }
    }

    private void UpdateShow()
    {
        SetGameState(GameState.WAIT_SHOW);
        _curVibrating = _sequence.ElementAt(_curSequence);
        Debug.Log("show sequence: " + (_curSequence + 1) + "/" + _sequence.Count() + " color=" + _curVibrating + " [" + DebugPrintArray(_sequence) + "]");
    }

    private void ResetClicks()
    {
        _musicNotes
            .Select(x => x.GetComponent<Clickable>())
            .Where(c => c != null)
            .ToList()
            .ForEach(c => c.ResetClicked());
    }

    // returns which note was clicked. If several are, returns an empty string. If none, returns null.
    private string WhichClicked()
    {
        var clicked = _musicNotes
            .Select(note => (note, note.GetComponent<Clickable>()))
            .Where(c => c.Item2 != null)
            .Where(c => c.Item2.IsClicked)
            .Select(c => GetNoteColor(c.note))
            .ToList();
        return clicked.Count() > 1 ? "" : clicked.FirstOrDefault();
    }

    private void UpdateWaitPlay()
    {
        StopVibrating();

        if (_curSequence == _sequence.Count() - 1)
        { /* stop */
            StartGameMode(GameState.WIN);
        }
        else
        {
            /* next tick, will use next color */
            _curSequence++;
            ResetClicks();
            SetGameState(GameState.PLAY);
        }
    }

    private void UpdatePlay()
    {
        var clicked = WhichClicked();
        ResetClicks();

        if (clicked == null)
        { // player didn't click yet
            return;
        }

        if (clicked == "")
        { // player clicked on several
            return;
        }

        var color = _sequence.ElementAt(_curSequence);
        Debug.Log("play sequence: " + (_curSequence + 1) + "/" + _sequence.Count() + " color=" + color + " clicked=" + clicked + " [" + DebugPrintArray(_sequence) + " ]");
        if (clicked == color)
        { // success!
            SetGameState(GameState.WAIT_PLAY);
            _curVibrating = color;
            Debug.Log("going to PLAY-vibrate: " + _curVibrating);
        }
        else
        { // failure! :(
            SetGameState(GameState.LOSS);
        }
    }

    private string DebugPrintArray<C>(IEnumerable<C> xs)
    {
        return xs.Select(x => x.ToString()).Aggregate("", (a, b) => a + " " + b);
    }
}
