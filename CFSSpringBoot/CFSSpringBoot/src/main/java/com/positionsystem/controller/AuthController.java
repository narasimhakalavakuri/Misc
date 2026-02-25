// package com.positionsystem.controller;

// import com.positionsystem.dto.LoginRequest;
// import com.positionsystem.dto.UserDto;
// import com.positionsystem.entity.User;
// import com.positionsystem.exception.ValidationException;
// import com.positionsystem.repository.UserRepository;
// import jakarta.validation.Valid;
// import lombok.AllArgsConstructor;
// import lombok.Data;
// import lombok.RequiredArgsConstructor;
// import org.springframework.http.ResponseEntity;
// import org.springframework.security.crypto.password.PasswordEncoder;
// import org.springframework.web.bind.annotation.*;

// @RestController
// @RequestMapping("/v1/auth")
// @RequiredArgsConstructor
// public class AuthController {
    
//     private final UserRepository userRepository;
//     private final PasswordEncoder passwordEncoder;
    
//     @PostMapping("/login")
//     public ResponseEntity<LoginResponse> login(@Valid @RequestBody LoginRequest request) {
//         User user = userRepository.findByUserid(request.getUsername())
//             .orElseThrow(() -> new ValidationException("Invalid credentials"));
        
//         if (!passwordEncoder.matches(request.getPassword(), user.getPwd())) {
//             throw new ValidationException("Invalid credentials");
//         }
        
//         UserDto userDto = new UserDto(
//             user.getId(),
//             user.getUserid(),
//             user.getDepartment(),
//             user.getAccessMask(),
//             user.getFullName(),
//             user.getEmail()
//         );
        
//         // Simple token for now (use JWT in production)
//         String token = "mock-jwt-token-" + user.getUserid();
        
//         LoginResponse response = new LoginResponse(token, userDto);
//         return ResponseEntity.ok(response);
//     }
    
//     @Data
//     @AllArgsConstructor
//     public static class LoginResponse {
//         private String token;
//         private UserDto user;
//     }
// }


package com.positionsystem.controller;

import com.positionsystem.dto.LoginRequest;
import com.positionsystem.dto.UserDto;
import com.positionsystem.entity.User;
import com.positionsystem.repository.UserRepository;
import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.RequiredArgsConstructor;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.util.HashMap;
import java.util.Map;

@RestController
@RequestMapping("/v1/auth")
@RequiredArgsConstructor
public class AuthController {

    private final UserRepository userRepository;

    @PostMapping("/login")
    public ResponseEntity<Object> login(@RequestBody LoginRequest request) {
        try {
            System.out.println("=== LOGIN ATTEMPT ===");
            System.out.println("Username: " + request.getUsername());
            System.out.println("Password: " + request.getPassword());

            User user = userRepository.findByUserid(request.getUsername())
                .orElseThrow(() -> new RuntimeException("User not found: " + request.getUsername()));

            System.out.println("User found in database");
            System.out.println("Stored password (encrypted): " + user.getPwd());
            
            // CORRECT ALGORITHM: Match Delphi's EncryptionEncrypt
            // From Delphi code: ssENCPWD = utl.EncryptionEncrypt(ssPWD, sKey)
            // where sKey = ssPWD + ssUSERID (uppercase)
            String encryptedPassword = encryptPasswordDelphi(request.getPassword(), request.getUsername());
            System.out.println("Calculated encrypted password: " + encryptedPassword);
            
            if (!user.getPwd().equalsIgnoreCase(encryptedPassword)) {
                System.out.println("Password mismatch!");
                System.out.println("Expected: " + user.getPwd());
                System.out.println("Got: " + encryptedPassword);
                throw new RuntimeException("Invalid credentials");
            }

            UserDto userDto = new UserDto(
                user.getId(),
                user.getUserid(),
                user.getDepartment(),
                user.getAccessMask(),
                user.getFullName(),
                user.getEmail()
            );

            String token = "jwt-token-" + user.getUserid() + "-" + System.currentTimeMillis();

            LoginResponse response = new LoginResponse(token, userDto);
            System.out.println("LOGIN SUCCESSFUL for: " + request.getUsername());
            return ResponseEntity.ok(response);

        } catch (Exception e) {
            System.err.println("LOGIN ERROR: " + e.getMessage());
            e.printStackTrace();
            Map<String, String> error = new HashMap<>();
            error.put("error", "Login failed: " + e.getMessage());
            return ResponseEntity.badRequest().body(error);
        }
    }
    
    /**
     * CORRECT IMPLEMENTATION - Matches Delphi's EncryptionEncrypt
     * 
     * Delphi code reference:
     * ssENCPWD := utl.EncryptionEncrypt(ssPWD, sKey);
     * where sKey = ssPWD + uppercase(ssUSERID)
     * 
     * Algorithm: XOR each password character with repeating key bytes
     * Then convert result to uppercase hex string
     */
    private String encryptPasswordDelphi(String password, String userid) {
        if (password == null || password.isEmpty()) {
            return "";
        }
        
        // Build key: password + userid (UPPERCASE)
        String key = password + userid.toUpperCase();
        
        // XOR operation
        StringBuilder encrypted = new StringBuilder();
        for (int i = 0; i < password.length(); i++) {
            char passwordChar = password.charAt(i);
            char keyChar = key.charAt(i % key.length());
            
            // XOR the two characters
            int xorResult = passwordChar ^ keyChar;
            
            // Convert to 2-digit hex (uppercase)
            encrypted.append(String.format("%02X", xorResult));
        }
        
        return encrypted.toString();
    }

    @GetMapping("/me")
    public ResponseEntity<Object> getCurrentUser(@RequestHeader(value = "Authorization", required = false) String token) {
        try {
            String username = "admin";
            if (token != null && token.startsWith("jwt-token-")) {
                String[] parts = token.split("-");
                if (parts.length >= 3) {
                    username = parts[2];
                }
            }
            
            User user = userRepository.findByUserid(username)
                .orElseThrow(() -> new RuntimeException("User not found"));

            UserDto userDto = new UserDto(
                user.getId(),
                user.getUserid(),
                user.getDepartment(),
                user.getAccessMask(),
                user.getFullName(),
                user.getEmail()
            );

            return ResponseEntity.ok(userDto);
        } catch (Exception e) {
            Map<String, String> error = new HashMap<>();
            error.put("error", e.getMessage());
            return ResponseEntity.badRequest().body(error);
        }
    }

    @Data
    @AllArgsConstructor
    public static class LoginResponse {
        private String token;
        private UserDto user;
    }
}
