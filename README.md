# FIAP Cloud Games (FCG)

FIAP Cloud Games (FCG) é uma API REST em .NET 8 desenvolvida para a Fase 1 do Tech Challenge. O serviço concentra o cadastro e a autenticação de usuários, administração de contas, catálogo de jogos, promoções e bibliotecas de jogos adquiridos.

Nesta fase, a FCG registra a propriedade de um jogo após a conclusão de um fluxo externo de compra. Processamento de compra, cobrança e pagamento não fazem parte desta API.

## Escopo Atual

A API da Fase 1 oferece:

- Cadastro de usuários com validação de nome, e-mail e senha segura.
- Login e emissão de JWT bearer.
- Autorização baseada nos papéis `User` e `Administrator`.
- Administração de usuários: listagem, consulta, alteração de papel e exclusão.
- Cadastro de jogos no catálogo por administradores.
- Consulta autenticada do catálogo e dos detalhes de um jogo.
- Cadastro administrativo de promoções.
- Consulta da biblioteca de jogos adquiridos pelo usuário autenticado.
- Associação de um jogo à própria biblioteca após a conclusão de uma compra externa.
- Associação administrativa de um jogo à biblioteca de um usuário específico.
- Projeto independente `FCG.Migrations` como único responsável pela evolução estrutural e inicialização controlada do banco de dados.
- Migration de seed com a conta administrativa inicial necessária para operar os endpoints administrativos.
- Documentação OpenAPI interativa por meio do Swagger UI.

### Limites da Fase 1

A associação de um jogo a uma biblioteca representa somente a concessão da propriedade do jogo dentro da FCG. A API não implementa checkout, meios de pagamento, cobrança ou confirmação financeira. Esses processos são considerados externos e, após sua conclusão, a FCG recebe apenas a operação necessária para registrar a associação entre usuário e jogo.

O cadastro de promoções também é administrativo nesta fase. A API registra a promoção e suas regras, mas não implementa um fluxo de compra nem o cálculo de preço final com desconto.

## Stack Tecnológica

- .NET 8 e ASP.NET Core Minimal APIs.
- C# com tipos de referência anuláveis habilitados.
- Entity Framework Core 8.
- SQLite como banco de dados local padrão.
- Autenticação JWT bearer.
- Swashbuckle para OpenAPI e Swagger UI.
- xUnit e testes de integração do ASP.NET Core.

## Estrutura do Repositório

```text
.
|-- src
|   |-- FCG.Api              # API HTTP, composição, OpenAPI e configuração
|   |-- FCG.Application      # Casos de uso, DTOs e orquestração
|   |-- FCG.Domain           # Entidades, regras e conceitos de negócio
|   |-- FCG.Infrastructure   # Persistência e implementações de infraestrutura
|   `-- FCG.Migrations       # Executável independente, migrations e seed do banco
|-- tests
|   `-- FCG.Tests            # Testes unitários e de integração
|-- docs
|   `-- diagrams             # Diagramas DDD e fluxos da Fase 1
`-- FCG.slnx                 # Arquivo da solução
```

## Pré-requisitos

- .NET SDK 8.0 ou superior.
- Opcional: `dotnet-ef` para criar e consultar migrações.

Instale a ferramenta de linha de comando do EF Core caso ela ainda não esteja disponível:

```powershell
dotnet tool install --global dotnet-ef --version 8.0.8
```

## Restaurar e Compilar

Na raiz do repositório:

```powershell
dotnet restore .\FCG.slnx
dotnet build .\FCG.slnx --no-restore
```

## Preparar o Banco de Dados

O banco deve ser preparado **antes** da execução da API. A API não executa `Database.Migrate`, não cria tabelas e não possui lógica de bootstrap de dados.

Execute o projeto independente de migrations:

```powershell
dotnet run --project .\src\FCG.Migrations\FCG.Migrations.csproj
```

Por padrão, o runner usa:

```text
Data Source=fcg.db
```

Uma connection string diferente pode ser informada por argumento:

```powershell
dotnet run --project .\src\FCG.Migrations\FCG.Migrations.csproj -- `
  --connection="Data Source=c:\dados\fcg.db"
```

ou pela variável de ambiente `FCG_CONNECTION_STRING`.

O runner aplica, em ordem, todas as migrations pendentes, incluindo a migration de seed.

### Seed inicial

A migration `SeedInitialData` cria a conta administrativa inicial:

- E-mail: `admin@example.com`
- Senha: `Admin123!`
- Papel: `Administrator`

Essas credenciais são destinadas exclusivamente ao ambiente de demonstração/desenvolvimento da Fase 1 e devem ser substituídas em um ambiente real.

## Executar a API

Depois de preparar o banco, inicie a API usando o perfil HTTP:

```powershell
dotnet run --project .\src\FCG.Api\FCG.Api.csproj --launch-profile http
```

Ou use o perfil HTTPS:

```powershell
dotnet run --project .\src\FCG.Api\FCG.Api.csproj --launch-profile https
```

Os perfis de execução estão configurados para abrir o Swagger automaticamente:

- HTTP: `http://localhost:5285/swagger`
- HTTPS: `https://localhost:7206/swagger`

Se o navegador não abrir automaticamente, acesse uma dessas URLs manualmente enquanto o processo da API estiver em execução.

## Configuração

A configuração padrão da API está em [`src/FCG.Api/appsettings.json`](src/FCG.Api/appsettings.json).

### Banco de Dados

A API usa a connection string `DefaultConnection`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=fcg.db"
}
```

A mesma base deve ser utilizada pelo projeto `FCG.Migrations`. A responsabilidade é separada: o projeto de migrations cria/evolui o schema e executa seeds; a API apenas utiliza o schema já preparado para suas operações normais de negócio.

### JWT

As configurações do JWT ficam na seção `Jwt`:

- `Issuer`: emissor do token.
- `Audience`: audiência esperada do token.
- `SigningKey`: chave usada para assinar o token.
- `ExpirationMinutes`: duração do token em minutos.

A chave presente no `appsettings.json` é destinada ao ambiente local de desenvolvimento. Em produção, use variáveis de ambiente ou um gerenciador seguro de segredos e não faça commit de credenciais reais.

## Documentação da API

O Swagger UI está disponível em `/swagger` e o documento OpenAPI em `/swagger/v1/swagger.json`.

Os endpoints possuem tags, resumos, descrições, contratos de resposta e códigos HTTP documentados no Swagger. As operações protegidas também apresentam o requisito de autenticação JWT.

### Endpoints disponíveis

| Método | Rota | Acesso | Finalidade |
| --- | --- | --- | --- |
| `POST` | `/api/auth/register` | Público | Cadastrar um novo usuário. |
| `POST` | `/api/auth/login` | Público | Autenticar um usuário e emitir um JWT. |
| `GET` | `/api/games` | Usuário ou administrador | Listar os jogos disponíveis no catálogo. |
| `GET` | `/api/games/{gameId}` | Usuário ou administrador | Consultar os detalhes de um jogo do catálogo. |
| `GET` | `/api/library/me` | Usuário ou administrador | Consultar a biblioteca da identidade autenticada. |
| `POST` | `/api/library/me/games/{gameId}` | Usuário ou administrador | Associar um jogo à própria biblioteca após um fluxo externo de compra. |
| `GET` | `/api/admin/users` | Administrador | Listar usuários cadastrados. |
| `GET` | `/api/admin/users/{userId}` | Administrador | Consultar um usuário específico. |
| `PATCH` | `/api/admin/users/{userId}/role` | Administrador | Alterar o papel de um usuário entre `User` e `Administrator`. |
| `DELETE` | `/api/admin/users/{userId}` | Administrador | Excluir uma conta de usuário. |
| `POST` | `/api/admin/users/{userId}/games/{gameId}` | Administrador | Associar um jogo à biblioteca de um usuário específico. |
| `POST` | `/api/admin/games` | Administrador | Cadastrar um jogo no catálogo. |
| `POST` | `/api/admin/promotions` | Administrador | Cadastrar uma promoção. |

### Comportamentos relevantes

- Tentativas de acessar operações protegidas sem JWT retornam `401 Unauthorized`.
- Um usuário autenticado sem papel administrativo recebe `403 Forbidden` ao acessar operações de administração.
- Consultas por identificadores inexistentes retornam `404 Not Found` quando aplicável.
- Um jogo não pode ser associado duas vezes à biblioteca do mesmo usuário; a tentativa duplicada retorna `409 Conflict`.
- Um administrador não pode alterar o próprio papel nem excluir a própria conta; essas operações retornam `409 Conflict`.
- O cadastro de usuário rejeita e-mails duplicados e dados que não atendam às regras de validação.

## Exemplos de Uso

### Login administrativo após o seed

```powershell
$login = @{
  email = "admin@example.com"
  password = "Admin123!"
} | ConvertTo-Json

$tokenResponse = Invoke-RestMethod -Method Post `
  -Uri "http://localhost:5285/api/auth/login" `
  -ContentType "application/json" `
  -Body $login

$adminHeaders = @{ Authorization = "Bearer $($tokenResponse.accessToken)" }
```

### Cadastro e login de usuário

```powershell
$register = @{
  name = "Maria Silva"
  email = "maria@example.com"
  password = "StrongPassword123!"
} | ConvertTo-Json

Invoke-RestMethod -Method Post `
  -Uri "http://localhost:5285/api/auth/register" `
  -ContentType "application/json" `
  -Body $register

$login = @{
  email = "maria@example.com"
  password = "StrongPassword123!"
} | ConvertTo-Json

$tokenResponse = Invoke-RestMethod -Method Post `
  -Uri "http://localhost:5285/api/auth/login" `
  -ContentType "application/json" `
  -Body $login

$headers = @{ Authorization = "Bearer $($tokenResponse.accessToken)" }
```

### Consultar o catálogo

```powershell
$catalog = Invoke-RestMethod -Method Get `
  -Uri "http://localhost:5285/api/games" `
  -Headers $headers
```

### Associar um jogo à própria biblioteca

A operação abaixo pressupõe que o processo externo de compra já foi concluído:

```powershell
$gameId = "00000000-0000-0000-0000-000000000000"

Invoke-RestMethod -Method Post `
  -Uri "http://localhost:5285/api/library/me/games/$gameId" `
  -Headers $headers
```

Depois da associação, a biblioteca pode ser consultada com:

```powershell
Invoke-RestMethod -Method Get `
  -Uri "http://localhost:5285/api/library/me" `
  -Headers $headers
```

No Swagger UI, use o botão **Authorize** e informe o token no formato `Bearer {token}`.

## Migrações do Entity Framework

Todas as migrations residem exclusivamente em `FCG.Migrations`. Esse projeto contém o `IDesignTimeDbContextFactory<AppDbContext>` e também funciona como executável para aplicação das migrations.

Para listar migrations:

```powershell
dotnet ef migrations list `
  --project .\src\FCG.Migrations\FCG.Migrations.csproj `
  --startup-project .\src\FCG.Migrations\FCG.Migrations.csproj
```

Para criar uma nova migration:

```powershell
dotnet ef migrations add NomeDaMigration `
  --project .\src\FCG.Migrations\FCG.Migrations.csproj `
  --startup-project .\src\FCG.Migrations\FCG.Migrations.csproj `
  --output-dir Migrations
```

Para aplicar as migrations, prefira o runner do próprio projeto:

```powershell
dotnet run --project .\src\FCG.Migrations\FCG.Migrations.csproj
```

Dessa forma, `FCG.Api` não depende do assembly de migrations e não possui autorização para alterar o schema durante sua inicialização.

## Testes

Execute toda a suíte de testes com:

```powershell
dotnet test .\FCG.slnx
```

A suíte contém testes unitários e de integração para, entre outros comportamentos:

- regras de cadastro e segurança de senha;
- autenticação e emissão de JWT;
- autorização de usuários e administradores;
- administração de usuários;
- cadastro e consulta do catálogo de jogos;
- cadastro de promoções;
- associação de jogos às bibliotecas;
- isolamento da biblioteca entre usuários;
- execução do runner de migrations e aplicação do seed;
- persistência e restrições do banco;
- contratos HTTP e cenários de erro relevantes.

Os testes automatizados são usados como especificações executáveis dos comportamentos da aplicação e como proteção contra regressões. O histórico de commits não deve ser interpretado, isoladamente, como evidência cronológica da ordem em que testes e implementação foram escritos.

## Arquitetura e Práticas de Engenharia

- Domain-Driven Design com separação explícita entre API, Application, Domain, Infrastructure e Migrations.
- Entidades e regras de negócio concentradas no domínio.
- Casos de uso e contratos de aplicação separados da camada HTTP.
- Repositórios usados como fronteiras de persistência das operações de negócio.
- Um único `AppDbContext` central para o modelo relacional.
- `FCG.Migrations` independente e responsável exclusivamente pela evolução do schema e pelos seeds controlados.
- `FCG.Api` não referencia `FCG.Migrations` e não executa migrations durante o startup.
- Consultas de leitura do catálogo usam `AsNoTracking`.
- Autorização baseada em policies (`UserOrAdministrator` e `AdministratorOnly`).
- Middlewares centralizados para logging das requisições e tratamento de exceções.
- Swagger/OpenAPI para documentação e exploração dos contratos da API.
- Testes automatizados para regras e fluxos críticos.

## Documentação DDD e Diagramas

Os diagramas Draw.io em [`docs/diagrams`](docs/diagrams) documentam os principais comandos, queries, invariantes, políticas de autorização, persistência e resultados dos fluxos implementados.

- [`fluxo-criacao-usuario.drawio`](docs/diagrams/fluxo-criacao-usuario.drawio): cadastro, autenticação e operações administrativas sobre usuários.
- [`fluxo-criacao-jogo.drawio`](docs/diagrams/fluxo-criacao-jogo.drawio): catálogo, biblioteca, associação de jogos e promoções.

Os arquivos são multipágina e podem ser abertos diretamente no [diagrams.net](https://app.diagrams.net/).

## Solução de Problemas

### O Swagger não abre

Confirme que a API ainda está em execução e acesse `/swagger` manualmente. O comportamento de abertura automática é controlado pelo `launchSettings.json` e só se aplica ao usar um perfil de execução pelo `dotnet run` ou por uma IDE.

### A API retorna erro indicando que uma tabela não existe

Execute o projeto `FCG.Migrations` antes de iniciar a API e confirme que migrations e API estão configuradas para a mesma base:

```powershell
dotnet run --project .\src\FCG.Migrations\FCG.Migrations.csproj
```

### As requisições JWT retornam `401 Unauthorized`

Verifique se o token é válido, não expirou e está sendo enviado no cabeçalho `Authorization` no formato `Bearer {token}`. Os endpoints administrativos também exigem o papel `Administrator`.

### Recebo `403 Forbidden` em um endpoint administrativo

O token é válido, mas a conta autenticada não possui o papel `Administrator`. Para demonstração local, aplique a migration de seed e utilize a conta administrativa documentada acima.

### As migrations não são encontradas

Use `FCG.Migrations` tanto no argumento `--project` quanto em `--startup-project`. O projeto possui sua própria factory de design-time e não depende da API para gerar ou aplicar migrations.

### A associação de um jogo retorna `409 Conflict`

A combinação usuário+jogo é única. Um jogo que já pertence à biblioteca daquele usuário não pode ser associado novamente.

## Licença

Este repositório ainda não possui uma licença pública definida.
