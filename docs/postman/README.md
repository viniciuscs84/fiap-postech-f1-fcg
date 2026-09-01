# Postman - FCG Fase 1

Esta pasta contém os artefatos para testar todos os endpoints da API FCG no Postman.

## Padronização em português brasileiro

Os nomes visíveis da coleção, pastas, requisições, ambiente e variáveis do Postman foram padronizados em português brasileiro. Apenas elementos que fazem parte do contrato técnico da API permanecem no formato original, como rotas HTTP, nomes de propriedades JSON e valores de enumeração (`User` e `Administrator`). Isso evita alterar o contrato da aplicação apenas por motivo de apresentação.

## Arquivos

- `FCG-Fase1.postman_collection.json`: coleção completa com todos os endpoints da Fase 1, com nomes e descrições em português brasileiro.
- `FCG-Local-PTBR.postman_environment.json`: ambiente local com URL, credenciais de demonstração e variáveis reutilizadas pela coleção.

## Pré-requisitos

Antes de executar a coleção:

1. Prepare o banco executando `FCG.Migrations`.
2. Inicie `FCG.Api` no perfil HTTP.
3. Importe a coleção e o ambiente no Postman.
4. Selecione o ambiente `FCG Local - Português (Brasil)`.

A URL padrão é `http://localhost:5285`.

O administrador de demonstração criado pela migration de seed utiliza:

- e-mail: `admin@example.com`
- senha: `Admin123!`

## Fluxo sugerido

A coleção foi organizada para permitir uma demonstração encadeada na própria ordem das pastas:

1. `Autenticação`: cadastrar o usuário e autenticar usuário e administrador.
2. `Administração de Jogos`: cadastrar dois jogos distintos.
3. `Catálogo de Jogos`: listar o catálogo e consultar um jogo por ID.
4. `Biblioteca de Jogos`: associar o primeiro jogo à própria biblioteca e consultar a biblioteca.
5. `Administração de Usuários`: consultar usuários, conceder o segundo jogo ao usuário, alterar/restaurar seu papel e, opcionalmente, excluí-lo ao final.
6. `Administração de Promoções`: cadastrar uma promoção.

São usados dois jogos diferentes para que os dois fluxos de associação possam ser demonstrados com sucesso:

- `idJogoCompraUsuario`: utilizado no endpoint de associação à própria biblioteca;
- `idJogoConcessaoAdministrador`: utilizado na concessão administrativa a um usuário específico.

Isso evita que o segundo fluxo retorne `409 Conflict` por tentar associar o mesmo jogo duas vezes ao mesmo usuário.

## Variáveis automáticas

A coleção utiliza exclusivamente o **ambiente selecionado** para armazenar valores gerados durante a execução. Os scripts usam `pm.environment.set(...)`, evitando conflito de precedência entre variáveis de coleção e de ambiente.

São preenchidas automaticamente:

- `tokenUsuario` após a autenticação do usuário;
- `tokenAdministrador` após a autenticação do administrador;
- `idUsuario` após o cadastro do usuário;
- `idJogoCompraUsuario` após o primeiro cadastro de jogo;
- `idJogoConcessaoAdministrador` após o segundo cadastro de jogo;
- `idPromocao` após o cadastro da promoção.

Também são utilizadas as variáveis configuráveis `urlBase`, `nomeUsuario`, `emailUsuario`, `senhaUsuario`, `emailAdministrador` e `senhaAdministrador`.

## Reexecução da coleção

Para uma demonstração totalmente limpa, recomenda-se executar a collection sobre um banco recém-criado pelas migrations. Caso a mesma base seja reutilizada, cadastros com o mesmo e-mail, códigos de promoção ou outros dados únicos podem retornar conflito conforme as regras da aplicação.

## Observação sobre aquisição

As operações de associação de jogos não implementam checkout, cobrança nem pagamento. Elas apenas registram que o jogo pertence à biblioteca do usuário depois que a transação externa tiver sido concluída.
