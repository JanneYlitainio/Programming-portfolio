using System;
using Jypeli;
using Jypeli.Assets;

namespace Kolikkogoblin;
/// @author  Janne Yli-Tainio
/// @version 1.1 / 12.1.2024
/// <summary>
/// Goblin pyrkii keräämään kaikki kolikot, jotta voi paeta luolasta.
/// </summary>
public class Kolikkogoblin : PhysicsGame
{
    private static readonly String[] Taso0 = {
                  "    EP                  ",
                  "                        ",
                  "    *                 M ",
                  "    X   *       *   XXXX",
                  "        XX      X       ",
                  "             X          ",    
                  "       X                ",
                  "    *                   ",
                  "    X                   ",
                  "                        ",
                  "      X   X    XX  *    ",
                  "                   X    ",
                  "                 X      ",
                  "   *           X        ",
                  "  XXX        X          ",
                  "        X               ",
                  "      WWWW    G         ",
                  };
    
    private static readonly String[] Taso1 = {
        "                        ",
        "                        ",
        "    EP                  ",
        "                        ",
        "                      M ",
        "  *            X  X XXXX",
        "  X X  *   XX           ",
        "       X                ",    
        "                *       ",
        "    X           X       ",
        "                    ww  ",
        "wX                      ",
        "    *                   ",
        "   XXXX   X             ",
        "            X   X       ",
        "                   *    ",
        "  *                X    ",
        "  X   X         XX      ",
        "  WWW  X  W  G    WWW   ",
    };
    
    private static readonly String[] Taso2 = {
        "    EP                  ",
        "                        ",
        "                        ",
        "                        ",
        "                        ",
        "*                     M ",
        "X    XXX  w X  X   XwXXX",
        " X                      ",
        "  wX                    ",    
        "    *                   ",
        "  wwX X  XXX  X   X     ",
        "                        ",
        "                     X  ",
        "    *              *    ",
        "   XXX   XXX   X   X   w",
        "                        ",
        "  X                     ",
        " *                      ",
        " XX                 *   ",
        "   X   X    X      XX   ",
        "   WWWWWWW  W G W  WWWWW",
    };

    private static readonly String[] Pelilapi =
    {
        "                        ",
        "                        ",
        "                        ",
        "                        ",
        "                        ",
        "                        ",
        "                        ",
    };
    
    private static readonly int TileWidth = 800 / Taso0[0].Length;
    private static readonly int TileHeight = 480 / Taso0.Length;
    private static readonly string[][] Tasolista = { Taso0, Taso1, Taso2, Pelilapi};
    
    private IntMeter _keratytKolikot;
    private IntMeter _elamat;
    private readonly IntMeter _tasoNr = new IntMeter(0, 0, 3);

    private static readonly SoundEffect Olioonsattuu = LoadSoundEffect("hitHurt");
    private static readonly SoundEffect Kolikkoaani = LoadSoundEffect("PickupCoin");
    private static readonly SoundEffect Seuraavakarttaaani = LoadSoundEffect("powerUp");
    
    private static readonly Image Taustakuva = LoadImage("tasonkuva1");
    
    /// <summary>
    /// Kun Peli käynnistetään ikkunasta voi valita haluaako vai eikö halua pelata.
    /// </summary>
    public override void Begin()
    {
        MultiSelectWindow alkuvalikko = new MultiSelectWindow("Pelin alkuvalikko", "Aloita peli","Lopeta");
        Add(alkuvalikko);
        alkuvalikko.AddItemHandler(0, AloitaUusiPeli);
        alkuvalikko.AddItemHandler(1, Exit);
        
    }
    /// <summary>
    /// Aloitetaan peli. Aluksi siivotaan kaikki, jotta voidaan aloittaa uusi peli.
    /// </summary>
    public void AloitaUusiPeli()
    {
        _tasoNr.Value = 0;
        UusiTaso();
    }
    /// <summary>
    /// Aloitetaan pelissä uusi taso. Kartta luodaan String[] taulukon avulla.
    /// </summary>
    public void UusiTaso()
    {
        ClearAll();
        if (_tasoNr.Value == 3)
        {
            MultiSelectWindow loppuvalikko = new MultiSelectWindow("Onneksi Olkoon läpäisit pelin", "Aloita uusi peli","Lopeta");
            Add(loppuvalikko);
            loppuvalikko.AddItemHandler(0, AloitaUusiPeli);
            loppuvalikko.AddItemHandler(1, Exit);
            return;
        }
        ClearAll();
        int index = _tasoNr.Value;
        string[] tasonKuva = Tasolista[index];
        Level.Background.Image = Taustakuva;
        
        TileMap tiles = TileMap.FromStringArray(tasonKuva);
        _tasoNr.Value++;
        Gravity = new Vector(0, -500);
        IsFullScreen = true;

        tiles.SetTileMethod('X', LuoPalikka, Color.Wheat);
        tiles.SetTileMethod('*', LuoKolikko, Color.Yellow);
        tiles.SetTileMethod('G', LuoGoblin, Color.Green);
        tiles.SetTileMethod('W', LuoPiikkeja, Color.DarkGray);
        tiles.SetTileMethod('M', LuoMaali, Color.Blue);
        tiles.SetTileMethod('P', LuoPistelaskuri, Color.Blue);
        tiles.SetTileMethod('E', LuoElamaLaskuri, Color.Blue);
        tiles.SetTileMethod('w', LuoLeijuviaPiikkeja, Color.Blue);

        tiles.Insert(TileWidth, TileHeight);
        tiles.Execute(TileWidth, TileHeight);
        
        MediaPlayer.Play("LuolaKuningas");
        
        Level.CreateBorders();
        Camera.ZoomToLevel();
        
        PhoneBackButton.Listen(ConfirmExit, "Lopeta peli");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");
    }
    
    
    /// <summary>
    /// Luo Palikan, jonka päällä voi liikkua.
    /// </summary>
    /// <param name="paikka">Kartassa määritetyt paikat joihin luodaan palikka</param>
    /// <param name="leveys">Taulukon yhden ruudun leveys</param>
    /// <param name="korkeus">Taulukon yhden ruudun korkeus</param>
    /// <param name="vari">Ppalikan väri mikäli sille ei olisi valittu kuvaa</param>
    private void LuoPalikka(Vector paikka, double leveys, double korkeus, Color vari)
    {
        PhysicsObject palikka = new PhysicsObject(leveys * 0.95 , korkeus / 2);
        palikka.Position = paikka;
        palikka.Color = vari;
        palikka.Tag = "rakenne";
        palikka.Image = LoadImage("palikka1");
        palikka.IgnoresPhysicsLogics = true;
        palikka.Mass = 100000;
        Add(palikka);
    }

    
    /// <summary>
    /// Luo kolikon, joita keräämällä voi kulkea portista.
    /// </summary>
    /// <param name="paikka">Kartassa määritetyt paikat joihin luodaan kolikko</param>
    /// <param name="leveys">Taulukon yhden ruudun leveys</param>
    /// <param name="korkeus">Taulukon yhden ruudun korkeus</param>
    /// <param name="vari">Olion vari kuvan alla</param>
    private void LuoKolikko(Vector paikka, double leveys, double korkeus, Color vari)
    {
        PhysicsObject kolikko = new PhysicsObject(leveys / 2, leveys / 2, Shape.Circle);
        kolikko.Position = paikka;
        kolikko.Color = vari;
        kolikko.Tag = "kolikko2";
        kolikko.Image = LoadImage("kolikko2");
        kolikko.IgnoresGravity = true;
        Add(kolikko);
    }


    /// <summary>
    /// Luo goblinin eli pelaajan.
    /// </summary>
    /// <param name="paikka">Kartassa määritetty paikka johon luodaan pelaaja</param>
    /// <param name="leveys">Taulukon yhden ruudun leveys</param>
    /// <param name="korkeus">Taulukon yhden ruudun korkeus</param>
    /// <param name="vari">Olion vari kuvan alla</param>
    private void LuoGoblin(Vector paikka, double leveys, double korkeus, Color vari)
    { 
        PlatformCharacter goblin = new PlatformCharacter(leveys * 0.8 , korkeus *1.5);
        goblin.Position = paikka;
        goblin.Color = vari;
        goblin.Image = LoadImage("goblinseisoo1");
        Add(goblin);
        
        Image[] goblinKavely = LoadImages( "goblinkavelee1","goblinkavelee1","goblinkavelee1","goblinkavelee1", "goblinkavelee2","goblinkavelee2","goblinkavelee2","goblinkavelee2", "goblinkavelee1","goblinkavelee1","goblinkavelee1","goblinkavelee1","goblinkavelee3", "goblinkavelee3","goblinkavelee3", "goblinkavelee3");
        Image goblinHyppy = LoadImage( "goblinhyppaa");
        Image goblinPaikallaan = LoadImage( "goblinseisoo1");
        
        goblin.AnimWalk = new Animation(goblinKavely);
        goblin.AnimIdle = new Animation(goblinPaikallaan);
        goblin.AnimJump = new Animation(goblinHyppy);
        
        Keyboard.Listen(Key.Left, ButtonState.Down, LiikutaPelaajaaVasemmalle, null, goblin);
        Keyboard.Listen(Key.Right, ButtonState.Down, LiikutaPelaajaaOikealle, null, goblin);
        Keyboard.Listen(Key.Up, ButtonState.Down, PelaajaHyppaa, null, new Vector(0, 2200), goblin);
        Keyboard.Listen(Key.Down, ButtonState.Down, LiikutaPelaajaaAlas, null, new Vector(0, -5000), goblin);
        
        AddCollisionHandler<PlatformCharacter, PhysicsObject>(goblin, LoytaaKolikon);
        AddCollisionHandler<PlatformCharacter, PhysicsObject>(goblin, LoytaaPortaalin);
        AddCollisionHandler<PlatformCharacter, PhysicsObject>(goblin, OsuuPiikkeihin);
        
    }
    
    
    /// Ohjelman avulla saadaan pelaaja hyppäämään.
    private void PelaajaHyppaa(Vector vektori,PlatformCharacter goblin)
    {
        goblin.Jump(110);
    }
    
    
    /// Pelaaja liikkuu oikealle.
    private void LiikutaPelaajaaOikealle(PlatformCharacter goblin)
    {
        goblin.Walk(80);
    }
    
    
    /// Pelaaja liikkuu Vasemmalle.
    private void LiikutaPelaajaaVasemmalle(PlatformCharacter goblin)
    {
        goblin.Walk(-80);
    }
    
    
    /// Pelaaja nopeuttaa laskeutumista.
    private void LiikutaPelaajaaAlas(Vector vektori, PlatformCharacter goblin)
    {
        goblin.Push(vektori);
    }
    
    
    /// <summary>
    /// Kolikon löytämisestä seuraavat asiat.
    /// </summary>
    /// <param name="goblin">pelaaja</param>
    /// <param name="kohde">kolikko tarkemmin mikä tahansa physichsobject, mutta ohjelman sisällä tarkennetaan nimenomaan kolikkoon</param>
    private void LoytaaKolikon(PlatformCharacter goblin, PhysicsObject kohde)
    {
        if (kohde.Tag.ToString() == "kolikko2")
        {
            AddCollisionHandler(kohde, goblin, CollisionHandler.DestroyObject);
            _keratytKolikot.Value += 1;
            Kolikkoaani.Play();
        }
    }


    /// <summary>
    /// Portaaliin pääsystä seuraavat asiat.
    /// </summary>
    /// <param name="goblin">Pelaaja</param>
    /// <param name="kohde">Portaali tarkemmin mikä tahansa physichsobject, mutta ohjelman sisällä tarkennetaan nimenomaan portaaliin</param>
    private void LoytaaPortaalin(PlatformCharacter goblin, PhysicsObject kohde)
    {
        if (kohde.Tag.ToString() == "Maali")
        {
            if (_keratytKolikot.Value == _keratytKolikot.MaxValue)
            {
                MessageDisplay.Add("läpäisit kartan");
                Seuraavakarttaaani.Play();
                UusiTaso();
            }
        }
    }
    
    
    /// <summary>
    /// Mitä tapahtuu kun pelaaja osuu piikkeihin. Pelaaja ottaa yhden iskun elämiinsä.
    /// </summary>
    /// <param name="goblin">Pelaaja</param>
    /// <param name="kohde">Piikit tarkemmin mikä tahansa physichsobject, mutta ohjelman sisällä tarkennetaan nimenomaan piikkeihin</param>
    private void OsuuPiikkeihin(PlatformCharacter goblin, PhysicsObject kohde)
    {
        if (kohde.Tag.ToString() == "piikkeja" || kohde.Tag.ToString() == "LeijuvatPiikit")
        {
            _elamat.Value -= 1;
            Olioonsattuu.Play();
            if (_elamat.Value == 0)
            {
                MessageDisplay.Add("Kuolit");
                Remove(goblin);
            }
        }
    }
    
    
    /// <summary>
    /// Luodaan kartalle piikit.
    /// </summary>
    /// <param name="paikka">Paikka kartassa</param>
    /// <param name="leveys">Taulukon yhden ruudun leveys</param>
    /// <param name="korkeus">Taulukon yhden ruudun korkeus</param>
    /// <param name="vari">Olion vari kuvan alla</param>
    private void LuoPiikkeja(Vector paikka, double leveys, double korkeus, Color vari)
        {
            PhysicsObject piikkeja = new PhysicsObject(leveys, 30);
            piikkeja.Position = paikka;
            Image piikkienkuva = LoadImage("piikit");
            piikkeja.Image = piikkienkuva;
            piikkeja.Tag = "piikkeja";
            piikkeja.IgnoresPhysicsLogics = true;
            piikkeja.Mass = 100000;
            Add(piikkeja);

        }
    
    
    /// <summary>
    /// Luodaan kartalle piikit, jotka eivät ole pohjakerroksessa.
    /// </summary>
    /// <param name="paikka">Paikka kartassa</param>
    /// <param name="leveys">Taulukon yhden ruudun leveys</param>
    /// <param name="korkeus">Taulukon yhden ruudun korkeus</param>
    /// <param name="vari">Olion vari kuvan alla</param>
    private void LuoLeijuviaPiikkeja(Vector paikka, double leveys, double korkeus, Color vari)
    {
        PhysicsObject leijuviapiikkeja = new PhysicsObject(leveys, 30);
        leijuviapiikkeja.Position = paikka;
        Image piikkienkuva = LoadImage("LeijuvatPiikit");
        leijuviapiikkeja.Image = piikkienkuva;
        leijuviapiikkeja.Tag = "piikkeja";
        leijuviapiikkeja.IgnoresPhysicsLogics = true;
        leijuviapiikkeja.Mass = 100000;
        Add(leijuviapiikkeja);

    } 
    
    
    /// <summary>
    /// LuoMaalin eli portaalin kartalle.
    /// </summary>
    /// <param name="paikka">Kartassa määritetty paikka, johon luodaan portaali</param>
    /// <param name="leveys">Taulukon yhden ruudun leveys</param>
    /// <param name="korkeus">Taulukon yhden ruudun korkeus</param>
    /// <param name="vari">Olion vari kuvan alla</param>
    private void LuoMaali(Vector paikka, double leveys, double korkeus, Color vari)
    {
        PhysicsObject portaali = new PhysicsObject(leveys * 1.5, korkeus * 2);
        portaali.Position = paikka;
        portaali.Color = vari;
        portaali.Tag = "Maali";
        portaali.Image = LoadImage("Portti");
        portaali.IgnoresPhysicsLogics = true;
        portaali.Mass = 10000;
        Add(portaali);
    }
    
    
    /// <summary>
    /// Luo pistelaskurin johon lasketaan kolikkojen määrä.
    /// </summary>
    /// <param name="paikka">Kartassa määritetty paikka pistelaskurille</param>
    /// <param name="leveys">Taulukon yhden ruudun leveys</param>
    /// <param name="korkeus">Taulukon yhden ruudun korkeus</param>
    /// <param name="vari">Tlion vari kuvan alla</param>
    private void LuoPistelaskuri(Vector paikka, double leveys, double korkeus, Color vari)
    {
        _keratytKolikot = new IntMeter(0);
        _keratytKolikot.MaxValue = 6;
        _keratytKolikot.UpperLimit += KaikkiKeratty;
        
        Label pistenaytto = new Label(); 
        pistenaytto.X = Screen.Left + 60;
        pistenaytto.Y = Screen.Top - 100;
        pistenaytto.TextColor = Color.Black;
        pistenaytto.Color = Color.Silver;
        pistenaytto.Title = "Kolikot:";

        pistenaytto.BindTo(_keratytKolikot);
        Add(pistenaytto);
    }
    
    
    /// Ilmoittaa kun kaikki kolikot on kerätty, että pelaaja voi kulkea portista.
    private void KaikkiKeratty()
    {
        MessageDisplay.Add("Voit nyt kulkea Portista");
    }
    
    
    /// <summary>
    /// Luo elämälaskurin, jonka avulla goblin voi kuolla piikkeihin.
    /// </summary>
    /// <param name="paikka">Kartassa määritetty paikka elämälaskurille</param>
    /// <param name="leveys">Taulukon yhden ruudun leveys</param>
    /// <param name="korkeus">Taulukon yhden ruudun korkeus</param>
    /// <param name="vari">Olion vari kuvan alla</param>
    private void LuoElamaLaskuri(Vector paikka, double leveys, double korkeus, Color vari)
    {
        _elamat = new IntMeter(3);
        
        Label elamat = new Label(); 
        elamat.X = Screen.Left + 160;
        elamat.Y = Screen.Top - 100;
        elamat.TextColor = Color.Black;
        elamat.Color = Color.Silver;
        elamat.Title = "Health:";

        elamat.BindTo(_elamat);
        Add(elamat);
    }
}  // Public class "Kolikkogoblin" ends
