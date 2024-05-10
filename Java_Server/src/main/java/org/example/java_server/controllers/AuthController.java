package org.example.java_server.controllers;

import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("/api/v1/Auth")
public class AuthController {

    @GetMapping("/SendEmailVerificationToken")
    public String getWeatherForecast() {
        return "Yooo javaspring";
    }
}
