// package com.positionsystem.controller;

// import com.positionsystem.dto.ApiResponse;
// import com.positionsystem.dto.UserDto;
// import com.positionsystem.service.UserService;
// import lombok.RequiredArgsConstructor;
// import org.springframework.http.ResponseEntity;
// import org.springframework.security.core.Authentication;
// import org.springframework.web.bind.annotation.*;
// import java.util.List;

// @RestController
// @RequestMapping("/v1/users")
// @RequiredArgsConstructor
// public class UserController {
    
//     private final UserService userService;
    
//     @GetMapping("/me")
//     public ResponseEntity<UserDto> getCurrentUser(Authentication authentication) {
//         UserDto user = userService.getUserByUserid(authentication.getName());
//         return ResponseEntity.ok(user);
//     }
    
//     @GetMapping
//     public ResponseEntity<List<UserDto>> getAllUsers() {
//         List<UserDto> users = userService.getAllUsers();
//         return ResponseEntity.ok(users);
//     }
    
//     @PostMapping("/change-password")
//     public ResponseEntity<ApiResponse<Void>> changePassword(
//             @RequestBody ChangePasswordRequest request,
//             Authentication authentication) {
//         userService.changePassword(
//             authentication.getName(),
//             request.getOldPassword(),
//             request.getNewPassword()
//         );
//         return ResponseEntity.ok(new ApiResponse<>(true, null, "Password changed successfully"));
//     }
    
//     @DeleteMapping("/{id}")
//     public ResponseEntity<ApiResponse<Void>> deleteUser(@PathVariable Long id) {
//         userService.deleteUser(id);
//         return ResponseEntity.ok(new ApiResponse<>(true, null, "User deleted successfully"));
//     }
    
//     private static class ChangePasswordRequest {
//         private String oldPassword;
//         private String newPassword;
        
//         public String getOldPassword() { return oldPassword; }
//         public void setOldPassword(String oldPassword) { this.oldPassword = oldPassword; }
//         public String getNewPassword() { return newPassword; }
//         public void setNewPassword(String newPassword) { this.newPassword = newPassword; }
//     }
// }


package com.positionsystem.controller;

import com.positionsystem.dto.ApiResponse;
import com.positionsystem.dto.UserDto;
import com.positionsystem.service.UserService;
import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;
import lombok.RequiredArgsConstructor;
import org.springframework.http.ResponseEntity;
import org.springframework.security.core.Authentication;
import org.springframework.web.bind.annotation.*;

import java.util.HashMap;
import java.util.List;
import java.util.Map;

@RestController
@RequestMapping("/v1/users")
@RequiredArgsConstructor
public class UserController {

    private final UserService userService;

    @GetMapping("/me")
    public ResponseEntity<Map<String, Object>> getCurrentUser(Authentication authentication) {
        try {
            String username = authentication != null ? authentication.getName() : "admin";
            UserDto user = userService.getUserByUserid(username);
            Map<String, Object> response = new HashMap<>();
            response.put("success", true);
            response.put("data", user);
            return ResponseEntity.ok(response);
        } catch (Exception e) {
            Map<String, Object> error = new HashMap<>();
            error.put("error", "Failed to fetch current user: " + e.getMessage());
            return ResponseEntity.status(500).body(error);
        }
    }

    @GetMapping
    public ResponseEntity<Map<String, Object>> getAllUsers() {
        try {
            List<UserDto> users = userService.getAllUsers();
            Map<String, Object> response = new HashMap<>();
            response.put("success", true);
            response.put("total", users.size());
            response.put("data", users);
            return ResponseEntity.ok(response);
        } catch (Exception e) {
            Map<String, Object> error = new HashMap<>();
            error.put("error", "Failed to fetch users: " + e.getMessage());
            return ResponseEntity.status(500).body(error);
        }
    }

    @PostMapping("/change-password")
    public ResponseEntity<Map<String, Object>> changePassword(
            @RequestBody ChangePasswordRequest request,
            Authentication authentication) {
        try {
            String username = authentication != null ? authentication.getName() : "admin";
            userService.changePassword(
                username,
                request.getOldPassword(),
                request.getNewPassword()
            );
            Map<String, Object> response = new HashMap<>();
            response.put("success", true);
            response.put("message", "Password changed successfully");
            return ResponseEntity.ok(response);
        } catch (Exception e) {
            Map<String, Object> error = new HashMap<>();
            error.put("success", false);
            error.put("message", "Failed to change password: " + e.getMessage());
            return ResponseEntity.status(500).body(error);
        }
    }

    @DeleteMapping("/{id}")
    public ResponseEntity<Map<String, Object>> deleteUser(@PathVariable Long id) {
        try {
            userService.deleteUser(id);
            Map<String, Object> response = new HashMap<>();
            response.put("success", true);
            response.put("message", "User deleted successfully");
            return ResponseEntity.ok(response);
        } catch (Exception e) {
            Map<String, Object> error = new HashMap<>();
            error.put("success", false);
            error.put("message", "Failed to delete user: " + e.getMessage());
            return ResponseEntity.status(500).body(error);
        }
    }

    @Data
    @NoArgsConstructor
    @AllArgsConstructor
    public static class ChangePasswordRequest {
        private String oldPassword;
        private String newPassword;
    }
}
