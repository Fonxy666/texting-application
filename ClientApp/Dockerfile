# Use an official Node runtime as the base image
FROM node:latest as build

# Set the working directory in the container
WORKDIR /app

# Copy package.json and package-lock.json to the working directory
COPY package*.json ./

# Install Angular CLI globally
RUN npm install -g @angular/cli

# Install dependencies
RUN npm install

# Copy the remaining application code to the working directory
COPY . .

# Build the Angular app for production
RUN ng build

# Use a lightweight Node image as a runtime
FROM nginx:alpine

# Copy the built app from the previous stage to the NGINX HTML directory
COPY --from=build /app/dist/client-app/browser /usr/share/nginx/html/

# Expose port 80 to the outside world
EXPOSE 80

# Command to run NGINX when the container starts
CMD ["nginx", "-g", "daemon off;"]