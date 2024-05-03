# Use an official Node.js as the base image
FROM node:alpine

# Set the working directory
WORKDIR /usr/src/app

# Copy package.json and package-lock.json
COPY package*.json ./

# Install Angular CLI
RUN npm install -g @angular/cli

# Install dependencies
RUN npm install

# Copy Angular app files
COPY . .

# Expose port 4200 for Angular (or any other port you prefer)
EXPOSE 4200

# Command to run Angular app
CMD ng serve --host 0.0.0.0 --port 4200 --disable-host-check --proxy-config proxy.conf.json