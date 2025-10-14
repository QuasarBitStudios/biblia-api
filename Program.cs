using System.Collections.Concurrent;
using System.Text.Json;
using static BibliaAPI.ResponseModels;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080);
});
string supabaseUrl = GetAmbientVariable("supabaseUrl");
string supabaseKey = GetAmbientVariable("supabaseKey");
var supabase = new Supabase.Client(supabaseUrl, supabaseKey);
await supabase.InitializeAsync();


var app = builder.Build();



var requestTimestamps = new ConcurrentDictionary<string, DateTime>();
var requestTimestampsUser = new ConcurrentDictionary<string, DateTime>();


string? GetAmbientVariable(string name)
{
    var value = Environment.GetEnvironmentVariable(name);
    if (string.IsNullOrEmpty(value))
    {
        Console.WriteLine($"Variável de ambiente {name} não encontrada.");
        return "invalid";
    }
    return value;
}

string? GetHeader(HttpRequest req,string name)
{
    if (!req.Headers.ContainsKey(name))
        return null;

    var header = req.Headers[name].ToString();
    if (!header.StartsWith("Bearer ") && name == "Authorization")
        return null;

    if (name != "Authorization")
        return header;
    else
        return header.Substring("Bearer ".Length).Trim();
}

app.Use(async (context, next) =>
{
var ip = context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
         ?? context.Connection.RemoteIpAddress?.ToString();
var method = context.Request.Method.ToLower();
if (!string.IsNullOrEmpty(ip))
{
        if (method == "get")
        {
            requestTimestamps.TryGetValue(ip, out DateTime lastRequest);

            var diff = DateTime.UtcNow - lastRequest;

            if (diff.TotalSeconds < 1)
            {
                context.Response.StatusCode = 429;
                await context.Response.WriteAsync("Opa, você esta com pressa? lembre-se dos limites da nossa api: quasarbit.com.br/bibliaapi.html");
                return;
            }
            requestTimestamps[ip] = DateTime.UtcNow;
        }
        else if (method == "post")
        {
            requestTimestampsUser.TryGetValue(ip, out DateTime lastRequest);

            var diff = DateTime.UtcNow - lastRequest;
            if (diff.TotalSeconds < 60)
            {
                context.Response.StatusCode = 429;
                await context.Response.WriteAsync("Aguarde 1 minuto para tentar novamente, fazemos isso para impedir spam/cadastros fantasma");
                return;
            }
            requestTimestampsUser[ip] = DateTime.UtcNow;
        }
    }

    await next();
});

app.MapPost("/cadastro", async (HttpRequest req) =>
{
    try
    {
        
        var body = await new StreamReader(req.Body).ReadToEndAsync();
        var cadastro = JsonSerializer.Deserialize<CadastroRequest>(body);

        if (cadastro == null || string.IsNullOrEmpty(cadastro.Email) || string.IsNullOrEmpty(cadastro.Nome))
            return Results.BadRequest(new { error = "Campos obrigatórios ausentes." });

        
        var existente = await supabase
            .From<Usuarios>()
            .Where(u => u.Email == cadastro.Email)
            .Single();

        if (existente != null)
        {
            return Results.Ok(new CadastroResponse { Uuid = "Email em uso" });
        }

        
        var novoUsuario = new Usuarios
        {
            Id = Guid.NewGuid(),
            Nome = cadastro.Nome,
            Email = cadastro.Email,
            Uso = cadastro.Uso,
            Perfil = cadastro.Perfil
        };

        await supabase.From<Usuarios>().Insert(novoUsuario);

        return Results.Ok(new CadastroResponse { Uuid = novoUsuario.Id.ToString() });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Erro em /cadastro: {ex.Message}");
        return Results.Problem("Erro interno ao cadastrar usuário.");
    }
});

app.MapGet("/versiculo/dia", async (HttpRequest req) =>
{
    string? uuid = GetHeader(req,"Authorization");
    if (string.IsNullOrEmpty(uuid)) return Results.Unauthorized();
 
    var versiculo = await supabase.Rpc<List<Retorno>>("get_versiculo_dia", new { p_user_id = uuid });
    var result = versiculo.FirstOrDefault();
    if(result == null)
        return Results.NotFound();

    return Results.Ok(result);
});

app.MapGet("/versiculo/aleatorio", async (HttpRequest req) =>
{
    string? uuid = GetHeader(req, "Authorization");
    string? tipo = GetHeader(req,"tipo");
    if (string.IsNullOrEmpty(uuid)) return Results.Unauthorized();

    var versiculo = await supabase.Rpc<List<Retorno>>("get_versiculo_aleatorio", new { p_user_id = uuid, p_tipo = tipo });
    var result = versiculo.FirstOrDefault();
    if (result == null)
        return Results.NotFound();
    return Results.Ok(result);
});

app.MapGet("/oracao/dia", async (HttpRequest req) =>
{
    string? uuid = GetHeader(req, "Authorization");
    if (string.IsNullOrEmpty(uuid)) return Results.Unauthorized();
 
    var oracao = await supabase.Rpc<List<Retorno>>("get_oracao_dia", new { p_user_id = uuid });
    var result = oracao.FirstOrDefault();
    if (result == null)
        return Results.NotFound();
    return Results.Ok(result);
});


app.Run();
