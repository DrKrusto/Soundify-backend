# Soundify Backend

Soundify Backend is the backend component of the Soundify mobile application. It provides the necessary APIs and functionality to support user authentication, image and sound uploads, and managing users' favorites.

## Description

Soundify is a mobile application built with Flutter that allows users to explore, play, and favorite sounds. The backend serves as the API server, handling user management, file uploads, and other related functionality.

## Features

- User creation and authentication
- Image and sound uploads
- Favorite sounds management

## Technologies Used

- ASP.NET Core
- Entity Framework Core
- SQLite
- Swagger (API documentation)
- JWT for authentication

## Installation

To run the Soundify backend locally, please ensure you have the following dependencies installed:

- .NET 7

Follow these steps to set up the backend:

1. Clone this repository to your local machine.
2. Open a terminal or command prompt and navigate to the project's root directory.
3. Run the following commands to apply database migrations:
   
   ```bash
   dotnet ef database update
4. Set the JwtSettings:SecretKey value in your user secrets. This can be done using the following command:
   
   ```bash
   dotnet user-secrets set "JwtSettings:SecretKey" "<your_secret_key>"
5. Build and run the project using the following command:
   
   ```bash
   dotnet run
The backend API will be accessible at https://localhost:7177 (or http://localhost:5101 for non-HTTPS).

## Usage

The Soundify backend is not hosted on a public server by default. However, you can host it yourself by deploying the application to a hosting environment of your choice. Ensure that the necessary dependencies are installed and the required configurations are set.

## API Documentation

The API documentation is automatically generated using Swagger. Once the backend is running, you can access the Swagger UI to explore and interact with the API endpoints. Open your web browser and navigate to https://localhost:7177/swagger (or http://localhost:5101/swagger for non-HTTPS).

## Contributing

This project is a student project and currently not open to external contributions. However, you are welcome to fork the repository and modify it for your own use.

## To-do (probably won't)

- Better handling of validation errors
- Adding CancellationToken
