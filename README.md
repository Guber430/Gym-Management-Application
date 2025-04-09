# Gym Management Application

This is a comprehensive **Gym Management Application** designed to help gyms manage group classes, instructors, and client enrollments efficiently. The application includes essential features like scheduling, CRUD operations, validation, error handling, sorting, filtering, paging, and more. It also incorporates security measures such as auditing and concurrency control to ensure data integrity and user accountability.

## Features

- **Group Class Scheduling**: Allows clients to view and enroll in various group classes.
- **Instructor Management**: Manages the gym's instructors, including their availability and scheduling.
- **Client Enrollments**: Enables clients to enroll in classes and manage their memberships.
- **CRUD Operations**: Full functionality to create, read, update, and delete data for clients, instructors, and classes.
- **Data Validation**: Ensures that only valid data is entered into the system.
- **Error Handling**: Includes database error handling to ensure smooth user interactions.
- **Sorting & Filtering**: Provides an intuitive interface for sorting and filtering data.
- **Paging**: Handles large sets of data by paginating results for easy navigation.
- **Security**: Incorporates **auditing** and **concurrency control** to enhance application security and prevent data conflicts.
  
## User Roles and Access

The application supports the following user roles, each with different levels of access:

- **Admin**: Full access to all features and management capabilities.
- **Supervisor**: Access to most features, but with limited administrative controls.
- **Staff**: Can manage day-to-day operations but cannot modify core settings.
- **Client**: Can view and enroll in classes but cannot access administrative features.
  
**Example Users**:  
- **Admin**: admin@outlook.com | Pa55w@rd  
- **Security**: security@outlook.com | Pa55w@rd  
- **Supervisor**: supervisor@outlook.com | Pa55w@rd  
- **Staff**: staff@outlook.com | Pa55w@rd  
- **Client**: client@outlook.com | Pa55w@rd  
- **User**: user@outlook.com | Pa55w@rd  

All users share the same password: `Pa55w@rd`.

For more details about user accessibility and permissions, refer to the attached "User-Authorization" picture.

## Live Demo

You can try the application live by visiting the following link:
[Gym Management Application Live Demo](https://gymmanagement20241129.azurewebsites.net)

## Technologies Used
- **Backend**: C#, .NET MVC
- **Database**: SQLite
- **Frontend**: HTML, CSS, JavaScript (for user interface)
- **Authentication**: Built-in security measures with role-based access control
- **Error Handling**: Includes proper exception handling and validation messages

## Setup Instructions

### Prerequisites:
- **.NET SDK**
- **SQLite**
- **Visual Studio or preferred IDE**

### Clone the Repository
```bash
git clone https://github.com/Guber430/Gym-Management-Application.git
cd Gym-Management-Application
