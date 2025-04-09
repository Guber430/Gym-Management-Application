# Gym Management Application

Welcome to the **Gym Management Application**, a comprehensive solution for managing group classes, instructors, and client enrollments in your gym. This application provides a user-friendly interface with robust functionality, allowing gym owners and staff to efficiently manage their daily operations.

---

## Table of Contents
- [Project Overview](#project-overview)
- [Features](#features)
- [User Roles and Access](#user-roles-and-access)
- [Live Demo](#live-demo)
- [Technologies Used](#technologies-used)
- [Setup Instructions](#setup-instructions)
  - [Prerequisites](#prerequisites)
  - [Clone the Repository](#clone-the-repository)
 
---

## Features

- **🏋️‍♂️ Group Class Scheduling**  
  Schedule and manage group classes with ease, allowing clients to view and enroll in available sessions.

- **👨‍🏫 Instructor Management**  
  Manage instructors, track their availability, and assign them to relevant classes.

- **📝 Client Enrollments**  
  Clients can enroll in available classes and manage their memberships, ensuring smooth operations.

- **🔧 CRUD Operations**  
  Full Create, Read, Update, and Delete operations for managing clients, instructors, and classes.

- **✅ Data Validation**  
  Ensures that only valid data is entered, minimizing errors and improving data integrity.

- **⚙️ Error Handling**  
  Robust database error handling for a smooth user experience.

- **🔍 Sorting & Filtering**  
  Users can easily sort and filter class schedules, instructor lists, and client data.

- **📑 Paging**  
  Handle large datasets with ease by paginating results for better navigation.

- **🔒 Security**  
  **Auditing** and **Concurrency Control** ensure data integrity and user accountability.

---

## User Roles and Access

The application supports multiple user roles, each with specific access levels:

| **Role**     | **Description**                                    | **Email**                    | **Password** |
|--------------|----------------------------------------------------|------------------------------|--------------|
| **Admin**    | Full access to all features and management controls | admin@outlook.com             | Pa55w@rd     |
| **Security** | Security management features                       | security@outlook.com          | Pa55w@rd     |
| **Supervisor**| Manage operations but with limited administrative control | supervisor@outlook.com       | Pa55w@rd     |
| **Staff**    | Can manage day-to-day operations but no access to core settings | staff@outlook.com            | Pa55w@rd     |
| **Client**   | Can view and enroll in classes                     | client@outlook.com            | Pa55w@rd     |
| **User**     | General user role with restricted access           | user@outlook.com              | Pa55w@rd     |

> **Note**: All users share the same password: `Pa55w@rd`.

For more details about user accessibility and permissions, refer to the attached **User-Authorization** image.

## Live Demo

Check out the live version of the application by visiting the link below:

[🔗 Gym Management Application Live Demo](https://gymmanagement20241129.azurewebsites.net)

## Technologies Used

- **Backend**: C#, .NET MVC
- **Database**: SQLite
- **Frontend**: HTML, CSS, JavaScript (for the user interface)
- **Authentication**: Built-in security measures with role-based access control
- **Error Handling**: Includes proper exception handling and validation messages

---

## Setup Instructions

### Prerequisites

Make sure you have the following installed:

- **.NET SDK**
- **SQLite**
- **Visual Studio** or your preferred IDE

### Clone the Repository

Clone the project from GitHub:
```bash
git clone https://github.com/Guber430/Gym-Management-Application.git
cd Gym-Management-Application
