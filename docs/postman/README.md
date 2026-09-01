# Postman - FCG Fase 1

Esta pasta contém os artefatos para testar todos os endpoints da API FCG no Postman.

## Arquivos

- `FCG-Phase1.postman_collection.json`: coleção completa com todos os endpoints da Fase 1.
- `FCG-Local.postman_environment.json`: ambiente local com URL, credenciais de demonstração e variáveis reutilizadas pela coleção.

## Pré-requisitos

Antes de executar a coleção:

1. Prepare o banco executando `FCG.Migrations`.
2. Inicie `FCG.Api` no perfil HTTP.
3. Importe a coleção e o ambiente no Postman.
4. Selecione o ambiente `FCG Local`.

A URL padrão é `http://localhost:5285`.

O administrador de demonstração criado pela migration de seed utiliza:

- e-mail: `admin@example.com`
- senha: `Admin123!`

## Fluxo sugerido

Para uma demonstração encadeada, execute as requisições aproximadamente nesta ordem:

1. `Cadastrar usuário`.
2. `Login usuário`.
3. `Login administrador`.
4. `Cadastrar jogo`.
5. `Listar jogos` e `Consultar jogo por ID`.
6. `Associar jogo à minha biblioteca` e `Consultar minha biblioteca`.
7. Operações de administração de usuários.
8. `Cadastrar promoção`.
9. `Excluir usuário` somente ao final, pois é uma operação destrutiva.

## Variáveis automáticas

A coleção armazena automaticamente:

- `userToken` após o login do usuário;
- `adminToken` após o login do administrador;
- `userId` após um cadastro bem-sucedido;
- `gameId` após o cadastro de um jogo;
- `promotionId` após o cadastro de uma promoção.

As demais requisições reutilizam essas variáveis, reduzindo a necessidade de copiar IDs e tokens manualmente.

## Observação sobre aquisição

As operações de associação de jogos não implementam checkout, cobrança nem pagamento. Elas apenas registram que o jogo pertence à biblioteca do usuário depois que a transação externa tiver sido concluída.
