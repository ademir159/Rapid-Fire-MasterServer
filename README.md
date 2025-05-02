# Rapid-Fire-MasterServer

MasterServer para Unity3D — Criado para o Projeto Rapid Fire.

Este servidor gerencia a infraestrutura central para partidas multiplayer, utilizando conexões TCP. Ele oferece os seguintes recursos principais:

## ✨ Funcionalidades

- Criação de conexões TCP com os clientes (jogadores)
- Registro de novos usuários
- Sistema de fila (queue) com modos de partida
- Leitura e escrita em banco de dados
- Criação de novos processos de servidores de partida (host) com base na quantidade de jogadores na fila
- Toda a lógica gerenciada centralmente pelo Master Server
- **Contém também a assembly para integração direta com projetos Unity3D**

## 🚀 Como usar

1. Clone este repositório:
   ```bash
   git clone https://github.com/ademir159/Rapid-Fire-MasterServer.git
   ```

2. Compile o projeto e execute o servidor.

3. Conecte seu cliente Unity3D ao MasterServer via TCP, utilizando a assembly fornecida.

## 📦 Requisitos

- .NET Framework ou .NET Core (dependendo da versão do projeto)
- Ambiente compatível com C++
- Banco de dados configurado (se aplicável)
- Unity3D para o cliente do jogo
- Linux ou Windows
## 📁 Estrutura do Projeto

- `Server/` – Contém o código principal do Master Server
- `Database/` – Scripts e conexões para acesso ao banco de dados
- `Utils/` – Funções auxiliares e manipuladores
- `QueueSystem/` – Sistema de filas e modos de jogo
- `UnityAssembly/` – Assembly pronta para uso no Unity3D

## 🛠️ Contribuindo

Contribuições são bem-vindas! Sinta-se à vontade para abrir uma *issue* ou enviar um *pull request*.

## 📄 Licença

Este projeto está licenciado sob a licença MIT.
