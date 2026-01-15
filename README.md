# ClipViewer

I wanted something like streamable/youtube where I upload my video clips and
it would last as long as the server stays up.

No plans to maintain this project as long as it fits my minimal needs

## 🚀 Tech Stack

### Backend
- **.NET 10.0**
- **Entity Framework Core**
- **PostgreSQL**
- **FFmpeg**
- **HLS (HTTP Live Streaming)**

### Frontend
- **Vue.js 3**
- **Tailwind v4**

## 🚀 Quick Start with Docker

1. **Clone the repository**
   ```bash
   git clone https://github.com/DevRuto/ClipViewer.git
   cd ClipViewer
   ```
   
2. **Start the application**
   ```bash
   docker compose up --build
   ```

3. **Access the application**
   - http://localhost:5000
   - Database: PostgreSQL on port 5432

4. Adding users
   - There's a `create_user.sh` script that would connect to the docker postgres db and create a user with the given username
   - There's also `update_user.sh` to change the API key for the given username if needed for any reason

## 🛠️ Manual Setup (Development)

### Backend

1. **Set up the database**
   - Create a new PostgreSQL database
   - Update the connection string in `appsettings.json`

2. **Run database migrations**
   ```bash
   cd ClipViewer.API
   ```

3. **Start the backend**
   ```bash
   dotnet run
   ```

### Frontend

1. **Install dependencies**
   ```bash
   cd clipviewer.vue
   npm install
   ```

2. **Start the development server**
   ```bash
   npm run dev
   ```


## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.


