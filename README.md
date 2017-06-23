# parent-child-chat-application

Parent-Child Chat Application is a course project written in C#. The project is a basic chat application between parents and their children. 

The intended use of the application is for parents to message their children and check what their children are doing on the computer. The application has two components: Client side and server side. At the server side, the server listens for connections from clients at a user-specified port. At the client side, users insert their names, the IP and port of the connection the server listens to and their role. Each user can either be a child or a parent. If a user is a child, then the user waits for a parent to connect him/her. If a user is a parent, then the user can send a request to server to connect a child. When the server receives a request, it creates a connection between parent and child. Over this connection, the parent and the child can communicate and the parent can take a screenshot of the child’s screen.

This is a sample project made in WPF. The project intends to demonstrate usage of WPF and socket connections in C#. 

The repository contains two branches: Client and Server. Each branch contains the necessary code to run the application. 
