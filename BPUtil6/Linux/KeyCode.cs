using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil.Linux
{
	/// <summary>
	/// Keys
	/// Most of the keys/buttons are modeled after USB HUT 1.12
	/// (see http://www.usb.org/developers/hidpage).
	/// Abbreviations in the comments:
	/// AC - Application Control
	/// AL - Application Launch Button
	/// SC - System Control
	/// </summary>
	public enum KeyCode
	{
		RESERVED = 0,
		ESC = 1,
		D1 = 2,
		D2 = 3,
		D3 = 4,
		D4 = 5,
		D5 = 6,
		D6 = 7,
		D7 = 8,
		D8 = 9,
		D9 = 10,
		D0 = 11,
		MINUS = 12,
		EQUAL = 13,
		BACKSPACE = 14,
		TAB = 15,
		Q = 16,
		W = 17,
		E = 18,
		R = 19,
		T = 20,
		Y = 21,
		U = 22,
		I = 23,
		O = 24,
		P = 25,
		LEFTBRACE = 26,
		RIGHTBRACE = 27,
		ENTER = 28,
		LEFTCTRL = 29,
		A = 30,
		S = 31,
		D = 32,
		F = 33,
		G = 34,
		H = 35,
		J = 36,
		K = 37,
		L = 38,
		SEMICOLON = 39,
		APOSTROPHE = 40,
		GRAVE = 41,
		LEFTSHIFT = 42,
		BACKSLASH = 43,
		Z = 44,
		X = 45,
		C = 46,
		V = 47,
		B = 48,
		N = 49,
		M = 50,
		COMMA = 51,
		DOT = 52,
		SLASH = 53,
		RIGHTSHIFT = 54,
		KPASTERISK = 55,
		LEFTALT = 56,
		SPACE = 57,
		CAPSLOCK = 58,
		F1 = 59,
		F2 = 60,
		F3 = 61,
		F4 = 62,
		F5 = 63,
		F6 = 64,
		F7 = 65,
		F8 = 66,
		F9 = 67,
		F10 = 68,
		NUMLOCK = 69,
		SCROLLLOCK = 70,
		KP7 = 71,
		KP8 = 72,
		KP9 = 73,
		KPMINUS = 74,
		KP4 = 75,
		KP5 = 76,
		KP6 = 77,
		KPPLUS = 78,
		KP1 = 79,
		KP2 = 80,
		KP3 = 81,
		KP0 = 82,
		KPDOT = 83,

		ZENKAKUHANKAKU = 85,
		_102ND = 86,
		F11 = 87,
		F12 = 88,
		RO = 89,
		KATAKANA = 90,
		HIRAGANA = 91,
		HENKAN = 92,
		KATAKANAHIRAGANA = 93,
		MUHENKAN = 94,
		KPJPCOMMA = 95,
		KPENTER = 96,
		RIGHTCTRL = 97,
		KPSLASH = 98,
		SYSRQ = 99,
		RIGHTALT = 100,
		LINEFEED = 101,
		HOME = 102,
		UP = 103,
		PAGEUP = 104,
		LEFT = 105,
		RIGHT = 106,
		END = 107,
		DOWN = 108,
		PAGEDOWN = 109,
		INSERT = 110,
		DELETE = 111,
		MACRO = 112,
		MUTE = 113,
		VOLUMEDOWN = 114,
		VOLUMEUP = 115,
		/// <summary>
		/// SC System Power Down
		/// </summary>
		POWER = 116,
		KPEQUAL = 117,
		KPPLUSMINUS = 118,
		PAUSE = 119,
		/// <summary>
		/// AL Compiz Scale (Expose)
		/// </summary>
		SCALE = 120,

		KPCOMMA = 121,
		HANGEUL = 122,
		HANGUEL = HANGEUL,
		HANJA = 123,
		YEN = 124,
		LEFTMETA = 125,
		RIGHTMETA = 126,
		COMPOSE = 127,

		/// <summary>
		/// AC Stop
		/// </summary>
		STOP = 128,
		AGAIN = 129,
		/// <summary>
		/// AC Properties
		/// </summary>
		PROPS = 130,
		/// <summary>
		/// AC Undo
		/// </summary>
		UNDO = 131,
		FRONT = 132,
		/// <summary>
		/// AC Copy
		/// </summary>
		COPY = 133,
		/// <summary>
		/// AC Open
		/// </summary>
		OPEN = 134,
		/// <summary>
		/// AC Paste
		/// </summary>
		PASTE = 135,
		/// <summary>
		/// AC Search
		/// </summary>
		FIND = 136,
		/// <summary>
		/// AC Cut
		/// </summary>
		CUT = 137,
		/// <summary>
		/// AL Integrated Help Center
		/// </summary>
		HELP = 138,
		/// <summary>
		/// Menu (show menu)
		/// </summary>
		MENU = 139,
		/// <summary>
		/// AL Calculator
		/// </summary>
		CALC = 140,
		SETUP = 141,
		/// <summary>
		/// SC System Sleep
		/// </summary>
		SLEEP = 142,
		/// <summary>
		/// System Wake Up
		/// </summary>
		WAKEUP = 143,
		/// <summary>
		/// AL Local Machine Browser
		/// </summary>
		FILE = 144,
		SENDFILE = 145,
		DELETEFILE = 146,
		XFER = 147,
		PROG1 = 148,
		PROG2 = 149,
		/// <summary>
		/// AL Internet Browser
		/// </summary>
		WWW = 150,
		MSDOS = 151,
		/// <summary>
		/// AL Terminal Lock/Screensaver
		/// </summary>
		COFFEE = 152,
		SCREENLOCK = COFFEE,
		/// <summary>
		/// Display orientation for e.g. tablets
		/// </summary>
		ROTATE_DISPLAY = 153,
		DIRECTION = ROTATE_DISPLAY,
		CYCLEWINDOWS = 154,
		MAIL = 155,
		/// <summary>
		/// AC Bookmarks
		/// </summary>
		BOOKMARKS = 156,
		COMPUTER = 157,
		/// <summary>
		/// AC Back
		/// </summary>
		BACK = 158,
		/// <summary>
		/// AC Forward
		/// </summary>
		FORWARD = 159,
		CLOSECD = 160,
		EJECTCD = 161,
		EJECTCLOSECD = 162,
		NEXTSONG = 163,
		PLAYPAUSE = 164,
		PREVIOUSSONG = 165,
		STOPCD = 166,
		RECORD = 167,
		REWIND = 168,
		/// <summary>
		/// Media Select Telephone
		/// </summary>
		PHONE = 169,
		ISO = 170,
		/// <summary>
		/// AL Consumer Control Configuration
		/// </summary>
		CONFIG = 171,
		/// <summary>
		/// AC Home
		/// </summary>
		HOMEPAGE = 172,
		/// <summary>
		/// AC Refresh
		/// </summary>
		REFRESH = 173,
		/// <summary>
		/// AC Exit
		/// </summary>
		EXIT = 174,
		MOVE = 175,
		EDIT = 176,
		SCROLLUP = 177,
		SCROLLDOWN = 178,
		KPLEFTPAREN = 179,
		KPRIGHTPAREN = 180,
		/// <summary>
		/// AC New
		/// </summary>
		NEW = 181,
		/// <summary>
		/// AC Redo/Repeat
		/// </summary>
		REDO = 182,

		F13 = 183,
		F14 = 184,
		F15 = 185,
		F16 = 186,
		F17 = 187,
		F18 = 188,
		F19 = 189,
		F20 = 190,
		F21 = 191,
		F22 = 192,
		F23 = 193,
		F24 = 194,

		PLAYCD = 200,
		PAUSECD = 201,
		PROG3 = 202,
		PROG4 = 203,
		/// <summary>
		/// AL Dashboard
		/// </summary>
		DASHBOARD = 204,
		SUSPEND = 205,
		/// <summary>
		/// AC Close
		/// </summary>
		CLOSE = 206,
		PLAY = 207,
		FASTFORWARD = 208,
		BASSBOOST = 209,
		/// <summary>
		/// AC Print
		/// </summary>
		PRINT = 210,
		HP = 211,
		CAMERA = 212,
		SOUND = 213,
		QUESTION = 214,
		EMAIL = 215,
		CHAT = 216,
		SEARCH = 217,
		CONNECT = 218,
		/// <summary>
		/// AL Checkbook/Finance
		/// </summary>
		FINANCE = 219,
		SPORT = 220,
		SHOP = 221,
		ALTERASE = 222,
		/// <summary>
		/// AC Cancel
		/// </summary>
		CANCEL = 223,
		BRIGHTNESSDOWN = 224,
		BRIGHTNESSUP = 225,
		MEDIA = 226,
		/// <summary>
		/// Cycle between available video outputs (Monitor/LCD/TV-out/etc)
		/// </summary>
		SWITCHVIDEOMODE = 227,
		KBDILLUMTOGGLE = 228,
		KBDILLUMDOWN = 229,
		KBDILLUMUP = 230,
		/// <summary>
		/// AC Send
		/// </summary>
		SEND = 231,
		/// <summary>
		/// AC Reply
		/// </summary>
		REPLY = 232,
		/// <summary>
		/// AC Forward Msg
		/// </summary>
		FORWARDMAIL = 233,
		/// <summary>
		/// AC Save
		/// </summary>
		SAVE = 234,
		DOCUMENTS = 235,

		BATTERY = 236,

		BLUETOOTH = 237,
		WLAN = 238,
		UWB = 239,

		UNKNOWN = 240,

		/// <summary>
		/// drive next video source
		/// </summary>
		VIDEO_NEXT = 241,
		/// <summary>
		/// drive previous video source
		/// </summary>
		VIDEO_PREV = 242,
		/// <summary>
		/// brightness up, after max is min
		/// </summary>
		BRIGHTNESS_CYCLE = 243,
		/// <summary>
		/// Set Auto Brightness: manual brightness control is off, rely on ambient
		/// </summary>
		BRIGHTNESS_AUTO = 244,
		BRIGHTNESS_ZERO = BRIGHTNESS_AUTO,
		/// <summary>
		/// display device to off state
		/// </summary>
		DISPLAY_OFF = 245,

		/// <summary>
		/// Wireless WAN (LTE, UMTS, GSM, etc.)
		/// </summary>
		WWAN = 246,
		WIMAX = WWAN,
		/// <summary>
		/// Key that controls all radios
		/// </summary>
		RFKILL = 247,

		/// <summary>
		/// Mute / unmute the microphone
		/// </summary>
		MICMUTE = 248,
	}
}
