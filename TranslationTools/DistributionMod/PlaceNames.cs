using System.Text.RegularExpressions;
using System.Collections.Concurrent;

namespace ScheduleISpanish;

public static class PlaceNames
{
    // Names extracted from region, property, building and named location fields.
    // Former Spanish labels are retained only to repair existing dictionary text.
    private static readonly ConcurrentDictionary<string,string> Names=new(StringComparer.OrdinalIgnoreCase)
    {
        ["Alleyway behind Slop Shop"]="Callejón detrás de Slop Shop",
        ["Alleyway behind Top Tattoo"]="Callejón detrás de Top Tattoo",
        ["Alleyway behind grocery store"]="Callejón detrás de la tienda de comestibles",
        ["Alleyway behind the laundromat"]="Callejón detrás de la lavandería",
        ["Alleyway next to HAM legal services"]="Callejón junto a los servicios jurídicos HAM",
        ["Apartment Buiding"]="Edificio de apartamentos",
        ["Arcade"]="Arcada",
        ["Barbershop"]="barbería",
        ["Barn"]="Barn",
        ["Basketball court"]="cancha de baloncesto",
        ["Behind Bank"]="detrás del banco",
        ["Behind Casino"]="detrás del casino",
        ["Behind Crimson Canary"]="Detrás del canario carmesí",
        ["Behind Gas-Mart"]="Detrás de Gas-Mart",
        ["Behind Handy Hank's"]="Detrás de Handy Hank",
        ["Behind Hyland Range"]="Detrás de la cordillera Hyland",
        ["Behind Randy's bait & tackle"]="Detrás del cebo y el aparejo de Randy",
        ["Behind Ray's Realty"]="Detrás de Ray's Realty",
        ["Behind Sauerkraut Supreme Pizzeria"]="Detrás de la pizzería suprema de chucrut",
        ["Behind Taco Ticklers"]="Detrás de los cosquillas de tacos",
        ["Behind Thompson Construction"]="Detrás de la construcción Thompson",
        ["Behind Thompson construction and demolition"]="Detrás de la construcción y demolición de Thompson",
        ["Behind Thrifty Threads"]="Detrás de los hilos ahorrativos",
        ["Behind auto shop"]="detrás del taller de autos",
        ["Behind bank"]="detrás del banco",
        ["Behind diner"]="detrás del restaurante",
        ["Behind fire station"]="Detrás de la estación de bomberos",
        ["Behind gas station"]="detrás de la gasolinera",
        ["Behind green house"]="Detrás de la casa verde",
        ["Behind medical practice"]="Detrás de la práctica médica",
        ["Behind motel office"]="Detrás de la oficina del motel",
        ["Behind pawn shop"]="Detrás de la casa de empeño",
        ["Behind the Crimson Canary"]="Detrás del canario carmesí",
        ["Behind the Post Office"]="Detrás de la oficina de correos",
        ["Behind the auto shop"]="Detrás del taller de autos",
        ["Behind the casino"]="detrás del casino",
        ["Behind the construction site"]="Detrás del sitio de construcción",
        ["Behind the dealership"]="Detrás del concesionario",
        ["Behind the shipping containers at the docks"]="Detrás de los contenedores en los muelles",
        ["Behind the suburbs bus stop"]="Detrás de la parada de autobús de las afueras",
        ["Behind the supermarket"]="detrás del supermercado",
        ["Behind the western Gas Mart"]="Detrás del Gas Mart occidental",
        ["Benji's Motel Room"]="Habitación de motel de Benji",
        ["Beth's Room"]="La habitación de Beth",
        ["Blue house"]="casa azul",
        ["Boutique Store"]="Tienda boutique",
        ["Brad's Tent"]="La tienda de Brad",
        ["Brick warehouse at the docks"]="Almacén de ladrillos en los muelles",
        ["Brown apartment block"]="Bloque de apartamentos marrón",
        ["Brown apartment block room #1"]="Habitación marrón del bloque de apartamentos #1",
        ["Bud's Bar"]="Bar de Bud",
        ["Bungalow"]="Bungalow",
        ["Cafe"]="café",
        ["Car Wash"]="Lavado de autos",
        ["Caravan"]="Caravana",
        ["Carl's House"]="La casa de Carl",
        ["Central canal"]="canal central",
        ["Charles' House"]="Casa de Carlos",
        ["Chelsey's hotel room"]="La habitación de hotel de Chelsey",
        ["Chemical Plant A"]="Planta química A",
        ["Chinese Restaurant"]="Restaurante chino",
        ["Church"]="Iglesia",
        ["Clothing Store"]="Tienda de ropa",
        ["Community Center"]="Centro Comunitario",
        ["Construction site"]="Sitio de construcción",
        ["Corner Store"]="Tienda de la esquina",
        ["Courthouse"]="Palacio de justicia",
        ["Dan's Hardware Upstairs"]="Hardware de Dan arriba",
        ["Destroyed RV"]="RV destruido",
        ["Docks"]="Muelles",
        ["Docks Industrial Building"]="Nave Industrial Muelles",
        ["Docks Shipping Container"]="Contenedor de envío de muelles",
        ["Docks Warehouse"]="Almacén de muelles",
        ["Downtown"]="Centro",
        ["Fire station"]="Estación de bomberos",
        ["Fish Warehouse"]="Almacén de pescado",
        ["Gas Mart"]="Mercado de gas",
        ["Gazebo"]="mirador",
        ["George and Molly's House"]="La casa de George y Molly",
        ["Graveyard"]="Cementerio",
        ["Grey docks building"]="Edificio de muelles grises",
        ["HAM Legal"]="JAMÓN Legal",
        ["Holt House"]="Casa Holt",
        ["Home"]="Inicio",
        ["Hyland Bank"]="Banco Hyland",
        ["Hyland Manor"]="Mansión Hyland",
        ["Hyland Medical"]="Médico Hyland",
        ["Hyland Medical Doctor's Office"]="Consultorio médico de Hyland",
        ["Hyland Point"]="Hyland Point",
        ["In front of motel"]="frente al motel",
        ["In the west construction site"]="En el sitio de construcción del oeste",
        ["Jane's Caravan"]="Caravana de Jane",
        ["Jerry's Tent"]="La tienda de Jerry",
        ["Jessi's Room"]="La habitación de Jessi",
        ["Kennedy House"]="Casa Kennedy",
        ["Knight House"]="Casa del Caballero",
        ["Kyle and Austin's House"]="La casa de Kyle y Austin",
        ["Laundromat"]="Lavandería",
        ["Leos' Shipping Container"]="Contenedor de envío de Leos",
        ["Les Ordures Puantes"]="Les Ordures Puantes",
        ["Mayor's House"]="Casa del alcalde",
        ["Medical clinic"]="clinica medica",
        ["Mick's House"]="La casa de Mick",
        ["Modern Mansion"]="Mansión Moderna",
        ["Motel Office"]="Oficina del motel",
        ["Motel Room"]="Habitación de motel",
        ["Motel Room 1"]="Habitación de motel 1",
        ["Next to Bud's Bar"]="Al lado del bar Bud",
        ["Next to Medical Center"]="Al lado del centro médico",
        ["Next to barbershop"]="Al lado de la barberia",
        ["Next to parking garage"]="Al lado del aparcamiento",
        ["Next to the courthouse"]="Al lado del juzgado",
        ["Next to the mayor's house"]="Al lado de la casa del alcalde",
        ["Next to the statue in the west park"]="Junto a la estatua en el parque oeste.",
        ["Nightclub"]="discoteca",
        ["North Industrial Building"]="Nave Industrial Norte",
        ["North Warehouse"]="Almacén Norte",
        ["North apartments"]="Apartamentos norte",
        ["North arcade wall"]="Pared de arcada norte",
        ["North waterfront"]="Costanera norte",
        ["Northtown"]="ciudad norte",
        ["Orgy Tent"]="Tienda De Orgía",
        ["Outside brown apartment block room #2"]="Habitación exterior del bloque de apartamentos marrón #2",
        ["Overpass Building"]="Edificio de paso elevado",
        ["Pawn Shop"]="Casa de empeño",
        ["Pawn shop"]="Casa de empeño",
        ["Pawn shop west wall"]="Pared oeste de la casa de empeño",
        ["Peter's Room"]="La habitación de Pedro",
        ["Pillville"]="pillville",
        ["Police Station"]="Comisaría de policía",
        ["Police station"]="comisaría",
        ["Post Office"]="Oficina de correos",
        ["RV"]="RV",
        ["Randy's Bait & Tackle"]="Cebo y aparejos de Randy",
        ["Residential park"]="parque residencial",
        ["Room 1"]="Habitación 1",
        ["Room 2"]="Habitación 2",
        ["Room 3"]="Habitación 3",
        ["Sauerkraut Supreme"]="Chucrut Supremo",
        ["Sewer Office"]="Oficina de alcantarillado",
        ["Shack"]="choza",
        ["Sherman House"]="Casa Sherman",
        ["Shipping Container"]="Contenedor de envío",
        ["Shipping containers near Sauerkraut Supreme"]="Contenedores marítimos cerca de Chucrut Supreme",
        ["Shooting Range"]="Campo de tiro",
        ["Skate park"]="parque de patinaje",
        ["Skatepark"]="parque de patinaje",
        ["Slop Shop"]="tienda de desperdicios",
        ["South Overpass"]="Paso elevado sur",
        ["South Overpass Building"]="Edificio del paso elevado sur",
        ["Stevenson House"]="Casa Stevenson",
        ["Storage Unit"]="Depósito",
        ["Storage Unit #2"]="Unidad de almacenamiento n.° 2",
        ["Storage Unit #3"]="Unidad de almacenamiento n.° 3",
        ["Suburbia"]="suburbios",
        ["Supermarket"]="supermercado",
        ["Sweatshop"]="Fábrica con explotación exagerada",
        ["Taco Ticklers"]="Cosquillas para tacos",
        ["Taco Ticklers exterior wall"]="Pared exterior de Taco Ticklers",
        ["Tall Tower"]="Torre alta",
        ["The Crimson Canary"]="El canario carmesí",
        ["The Piss Hut"]="La cabaña de orina",
        ["Thompson Construction & Demo"]="Construcción y demostración de Thompson",
        ["Thompson House"]="Casa Thompson",
        ["Town hall"]="ayuntamiento",
        ["Town hall fountain"]="fuente del ayuntamiento",
        ["Under the north end of the overpass"]="Debajo del extremo norte del paso elevado.",
        ["Under the west bridge"]="Debajo del puente oeste",
        ["Under west bridge"]="Debajo del puente oeste",
        ["Upscale Apartments"]="Apartamentos de lujo",
        ["Uptown"]="zona alta",
        ["Webster House"]="Casa Webster",
        ["Wei's Cabin"]="La cabaña de Wei",
        ["West Gas-Mart"]="West Gas-Mart",
        ["West Warehouse Wharf"]="Muelle del almacén oeste",
        ["Western wharf"]="muelle occidental",
        ["Westville"]="Westville",
        ["Wharf near french restaurant"]="Muelle cerca de restaurante francés",
        ["Wilkinson House"]="Casa Wilkinson",
    };
    private static readonly object RegistrationLock=new();
    private static volatile Regex Pattern=BuildPattern();
    private static Regex BuildPattern()=>new(@"(?<![\p{L}\p{N}_])(?:"+string.Join("|",Names.Keys.OrderByDescending(n=>n.Length).Select(Regex.Escape))+@")(?![\p{L}\p{N}_])",RegexOptions.CultureInvariant|RegexOptions.IgnoreCase,TimeSpan.FromMilliseconds(20));
    public static void Register(string name)
    {
        if(string.IsNullOrWhiteSpace(name) || name.Length>200 || name.Contains('\n')) return;
        lock(RegistrationLock)
        {
            if(Names.Count>=512 || !Names.TryAdd(name,name)) return;
            Pattern=BuildPattern();
        }
    }
    public static bool IsName(string source)=>Names.ContainsKey(source.Trim());
    public static string Protect(string source,Func<string,string> protect)=>Pattern.Replace(source,m=>protect(m.Value));
    public static bool Preserved(string source,string value)
    {
        var expected=Pattern.Matches(source).Cast<Match>().GroupBy(m=>m.Value,StringComparer.OrdinalIgnoreCase);
        var actual=Pattern.Matches(value).Cast<Match>().Select(m=>m.Value).ToArray();
        return expected.All(group=>actual.Count(v=>string.Equals(v,group.Key,StringComparison.OrdinalIgnoreCase))>=group.Count());
    }
    public static string Restore(string source,string value)
    {
        if(IsName(source)) return source;
        if(source==value || Preserved(source,value)) return value;
        foreach(Match match in Pattern.Matches(source))
        {
            string alias=Names[match.Value];
            if(string.Equals(alias,match.Value,StringComparison.OrdinalIgnoreCase)) continue;
            value=Regex.Replace(value,@"(?<![\p{L}\p{N}_])"+Regex.Escape(alias)+@"(?![\p{L}\p{N}_])",_=>match.Value,RegexOptions.IgnoreCase|RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(20));
        }
        // A changed name must never become a different destination.
        return Preserved(source,value) ? value : source;
    }
}
