# FIAP Cloud Games (FCG)

FIAP Cloud Games (FCG) é uma API REST em .NET 8 para a primeira fase de um marketplace de jogos digitais. O escopo atual contempla contas de usuários, autenticação JWT, gerenciamento administrativo do catálogo, promoções e a biblioteca de jogos adquiridos pelo usuário autenticado.

## Escopo Atual

A API da Fase 1 oferece:

- Cadastro e login de usuários.
- Autenticação com JWT bearer e autorização baseada em funções.
- Cadastro de jogos no catálogo por administradores.
- Cadastro de promoções por administradores.
- Consulta autenticada da biblioteca de jogos adquiridos pelo usuário atual.
- Aplicação automática das migrações do banco na inicialização da API.
- Documentação OpenAPI interativa por meio do Swagger UI.

A solução está intencionalmente limitada às capacidades aprovadas para a Fase 1. Processamento de pagamentos, sincronização com calendários ou provedores externos, videoconferência e capacidades futuras de automação não fazem parte da implementação atual.

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
|   |-- FCG.Api             # API HTTP, composição, OpenAPI e configuração
|   |-- FCG.Application     # Casos de uso, DTOs e orquestração
|   |-- FCG.Domain          # Entidades, regras e conceitos de negócio
|   |-- FCG.Infrastructure   # Persistência e implementações de infraestrutura
|   `-- FCG.Migrations       # Assembly dedicado às migrações do EF Core
|-- tests
|   `-- FCG.Tests           # Testes unitários e de integração
|-- docs                    # Documentação do produto e do projeto
|-- sdd                     # Especificações, issues e checklists de implementação
`-- FCG.slnx                # Arquivo da solução
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

## Executar a API

Inicie a API usando o perfil HTTP:

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

A configuração padrão está em [`src/FCG.Api/appsettings.json`](C:/Users/vinic/OneDrive/Documentos/ChatGPT/Pos%20-%20Challenge%201/src/FCG.Api/appsettings.json).

### Banco de Dados

A connection string padrão usa um arquivo SQLite local:

```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=fcg.db"
}
```

A API aplica as migrações pendentes do EF Core durante a inicialização. O arquivo do banco é criado no diretório de trabalho da API quando ainda não existe.

### JWT

As configurações do JWT ficam na seção `Jwt`:

- `Issuer`: emissor do token.
- `Audience`: audiência esperada do token.
- `SigningKey`: chave usada para assinar o token.
- `ExpirationMinutes`: duração do token em minutos.

Em produção, substitua a chave de desenvolvimento por variáveis de ambiente ou por um gerenciador seguro de segredos. Não faça commit de segredos de produção no `appsettings.json`.

### Administrador Inicial

É possível criar um administrador inicial na inicialização definindo `BootstrapAdmin:Enabled` como `true` e informando nome, e-mail e senha. Esse recurso é destinado à configuração local e a ambientes controlados; implantações de produção devem usar um processo aprovado de gerenciamento de segredos.

## Documentação da API

O Swagger UI está disponível em `/swagger` e o documento OpenAPI está disponível em `/swagger/v1/swagger.json`.

A documentação da API está escrita para usuários brasileiros, enquanto rotas, propriedades de DTOs, valores de enum e identificadores internos permanecem neutros em relação ao idioma.

Endpoints disponíveis:

| Método | Rota | Acesso | Finalidade |
| --- | --- | --- | --- |
| `POST` | `/api/auth/register` | Público | Cadastrar um usuário. |
| `POST` | `/api/auth/login` | Público | Autenticar e emitir um JWT. |
| `GET` | `/api/library/me` | Usuário autenticado ou administrador | Retornar a biblioteca do usuário atual. |
| `POST` | `/api/admin/games` | Administrador | Cadastrar um jogo no catálogo. |
| `POST` | `/api/admin/promotions` | Administrador | Cadastrar uma promoção. |

### Exemplo: Cadastro e Login

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

$headers = @{ Authorization = "Bearer $($tokenResponse.token)" }
Invoke-RestMethod -Method Get `
  -Uri "http://localhost:5285/api/library/me" `
  -Headers $headers
```

No Swagger UI, use o botão **Authorize** e informe o token no formato `Bearer {token}`.

## Migrações do Entity Framework

As migrações estão isoladas no projeto `FCG.Migrations`. Use esse projeto como projeto de migrações e `FCG.Api` como projeto de inicialização:

```powershell
dotnet ef migrations list `
  --project .\src\FCG.Migrations\FCG.Migrations.csproj `
  --startup-project .\src\FCG.Api\FCG.Api.csproj
```

Para aplicar as migrações manualmente:

```powershell
dotnet ef database update `
  --project .\src\FCG.Migrations\FCG.Migrations.csproj `
  --startup-project .\src\FCG.Api\FCG.Api.csproj
```

A API também aplica automaticamente as migrações pendentes ao ser iniciada.

## Testes

Execute toda a suíte de testes com:

```powershell
dotnet test .\FCG.slnx
```

O projeto de testes contém cobertura unitária e de integração para autenticação, autorização, catálogo, promoções, persistência, migrações e contratos da API.

## Arquitetura e Práticas de Engenharia

- Domain-Driven Design com camadas arquiteturais explícitas.
- Coesão por feature no projeto de Domínio sem eliminar a separação entre camadas.
- Um único `DbContext` central da aplicação.
- Assembly dedicado para versionamento das migrações.
- TDD para novos comportamentos de domínio e correções de regressão sempre que aplicável.
- Valores de máquina estáveis e neutros em relação ao idioma, com textos voltados ao usuário em português brasileiro.
- Regras de negócio mantidas fora das camadas de apresentação e transporte.

## Documentação do Projeto

- [`docs/ROADMAP.md`](C:/Users/vinic/OneDrive/Documentos/ChatGPT/Pos%20-%20Challenge%201/docs/ROADMAP.md): roadmap do produto e direcionamento das fases.
- [`sdd/agents/ROADMAP.md`](C:/Users/vinic/OneDrive/Documentos/ChatGPT/Pos%20-%20Challenge%201/sdd/agents/ROADMAP.md): sequência técnica e orientações de SDD.
- [`sdd/features`](C:/Users/vinic/OneDrive/Documentos/ChatGPT/Pos%20-%20Challenge%201/sdd/features): especificações de features e checklists de tarefas.
- [`docs`](C:/Users/vinic/OneDrive/Documentos/ChatGPT/Pos%20-%20Challenge%201/docs): diagramas e documentação complementar.

## Solução de Problemas

### O Swagger não abre

Confirme que a API ainda está em execução e acesse `/swagger` manualmente. O comportamento de abertura automática é controlado pelo `launchSettings.json` e só se aplica ao usar um perfil de execução pelo `dotnet run` ou por uma IDE.

### As requisições JWT retornam `401 Unauthorized`

Verifique se o token é válido, não expirou e está sendo enviado no cabeçalho `Authorization` no formato `Bearer {token}`. Os endpoints administrativos também exigem a função de administrador.

### As migrações não são encontradas

Use `FCG.Migrations` no argumento `--project` e `FCG.Api` no argumento `--startup-project`. Não execute os comandos do EF apontando para `FCG.Infrastructure`, pois as migrações foram separadas intencionalmente.

### O administrador inicial não é criado

Verifique se `BootstrapAdmin:Enabled` está definido como `true` e se nome, e-mail e senha foram configurados. A API não cria um administrador inicial quando o recurso está desabilitado.

## Licença

Este repositório ainda não possui uma licença pública definida.
