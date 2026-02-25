// package com.positionsystem.controller;

// import com.positionsystem.dto.ApiResponse;
// import com.positionsystem.entity.Department;
// import com.positionsystem.service.SystemService;
// import lombok.RequiredArgsConstructor;
// import org.springframework.http.ResponseEntity;
// import org.springframework.web.bind.annotation.*;
// import java.util.List;

// @RestController
// @RequestMapping("/v1/system")
// @RequiredArgsConstructor
// public class SystemController {
    
//     private final SystemService systemService;
    
//     @PostMapping("/close-day")
//     public ResponseEntity<ApiResponse<Void>> closeDay(@RequestBody CloseDayRequest request) {
//         systemService.closeDay(request.getDepartment(), request.getVerifyDate());
//         return ResponseEntity.ok(new ApiResponse<>(true, null, "Day closed successfully"));
//     }
    
//     @PostMapping("/reopen-day/{department}")
//     public ResponseEntity<ApiResponse<Void>> reopenDay(@PathVariable String department) {
//         systemService.reopenDay(department);
//         return ResponseEntity.ok(new ApiResponse<>(true, null, "Day reopened successfully"));
//     }
    
//     @GetMapping("/departments")
//     public ResponseEntity<List<Department>> getDepartments() {
//         List<Department> departments = systemService.getAllDepartments();
//         return ResponseEntity.ok(departments);
//     }
    
//     @GetMapping("/department-status/{department}")
//     public ResponseEntity<DepartmentStatusResponse> getDepartmentStatus(@PathVariable String department) {
//         boolean isClosed = systemService.isDepartmentClosed(department);
//         return ResponseEntity.ok(new DepartmentStatusResponse(isClosed));
//     }
    
//     private static class CloseDayRequest {
//         private String department;
//         private String verifyDate;
        
//         public String getDepartment() { return department; }
//         public void setDepartment(String department) { this.department = department; }
//         public String getVerifyDate() { return verifyDate; }
//         public void setVerifyDate(String verifyDate) { this.verifyDate = verifyDate; }
//     }
    
//     private static class DepartmentStatusResponse {
//         private final boolean isClosed;
        
//         public DepartmentStatusResponse(boolean isClosed) {
//             this.isClosed = isClosed;
//         }
        
//         public boolean isClosed() { return isClosed; }
//     }
// }


package com.positionsystem.controller;

import com.positionsystem.entity.Department;
import com.positionsystem.service.SystemService;
import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;
import lombok.RequiredArgsConstructor;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.stream.Collectors;

@RestController
@RequestMapping("/v1/system")
@RequiredArgsConstructor
public class SystemController {

    private final SystemService systemService;

    // ===================================================================
    // CONTROL ENDPOINTS
    // ===================================================================

    @PostMapping("/control")
    public ResponseEntity<Map<String, Object>> systemControl(
            @RequestBody SystemControlRequest request) {
        try {
            Map<String, Object> response = new HashMap<>();
            response.put("success", true);
            response.put("message", "System control request processed");
            response.put("action", request.getAction());
            return ResponseEntity.ok(response);
        } catch (Exception e) {
            Map<String, Object> error = new HashMap<>();
            error.put("success", false);
            error.put("message", "Failed to process system control: " + e.getMessage());
            return ResponseEntity.status(500).body(error);
        }
    }

    @PostMapping("/close-day")
    public ResponseEntity<Map<String, Object>> closeDay(
            @RequestBody CloseDayRequest request) {
        try {
            systemService.closeDay(request.getDepartment(), request.getVerifyDate());
            Map<String, Object> response = new HashMap<>();
            response.put("success", true);
            response.put("message", "Day closed successfully");
            response.put("department", request.getDepartment());
            response.put("verifyDate", request.getVerifyDate());
            return ResponseEntity.ok(response);
        } catch (Exception e) {
            Map<String, Object> error = new HashMap<>();
            error.put("success", false);
            error.put("message", "Failed to close day: " + e.getMessage());
            return ResponseEntity.status(500).body(error);
        }
    }

    @PostMapping("/reopen-day/{department}")
    public ResponseEntity<Map<String, Object>> reopenDay(
            @PathVariable String department) {
        try {
            systemService.reopenDay(department);
            Map<String, Object> response = new HashMap<>();
            response.put("success", true);
            response.put("message", "Day reopened successfully");
            response.put("department", department);
            return ResponseEntity.ok(response);
        } catch (Exception e) {
            Map<String, Object> error = new HashMap<>();
            error.put("success", false);
            error.put("message", "Failed to reopen day: " + e.getMessage());
            return ResponseEntity.status(500).body(error);
        }
    }

    // ===================================================================
    // DEPARTMENT ENDPOINTS
    // ===================================================================

    @GetMapping("/departments")
    public ResponseEntity<Map<String, Object>> getDepartments() {
        try {
            List<Department> departments = systemService.getAllDepartments();
            Map<String, Object> response = new HashMap<>();
            response.put("success", true);
            response.put("total", departments.size());
            response.put("data", departments.stream()
                .map(d -> Map.of(
                    "code", (Object) d.getCode(),
                    "description", (Object) d.getDescription(),
                    "isClosed", (Object) (d.getIsClosed() != null ? d.getIsClosed() : false)
                ))
                .collect(Collectors.toList()));
            return ResponseEntity.ok(response);
        } catch (Exception e) {
            Map<String, Object> error = new HashMap<>();
            error.put("error", "Failed to fetch departments: " + e.getMessage());
            return ResponseEntity.status(500).body(error);
        }
    }

    @GetMapping("/department-status/{department}")
    public ResponseEntity<Map<String, Object>> getDepartmentStatus(
            @PathVariable String department) {
        try {
            boolean isClosed = systemService.isDepartmentClosed(department);
            Map<String, Object> response = new HashMap<>();
            response.put("success", true);
            response.put("department", department);
            response.put("isClosed", isClosed);
            response.put("status", isClosed ? "CLOSED" : "OPEN");
            return ResponseEntity.ok(response);
        } catch (Exception e) {
            Map<String, Object> error = new HashMap<>();
            error.put("error", "Failed to fetch department status: " + e.getMessage());
            return ResponseEntity.status(500).body(error);
        }
    }

    // ===================================================================
    // REQUEST/RESPONSE DTOs
    // ===================================================================

    @Data
    @NoArgsConstructor
    @AllArgsConstructor
    public static class SystemControlRequest {
        private String action;
        private String details;
    }

    @Data
    @NoArgsConstructor
    @AllArgsConstructor
    public static class CloseDayRequest {
        private String department;
        private String verifyDate;
    }

    @Data
    @NoArgsConstructor
    @AllArgsConstructor
    public static class DepartmentStatusResponse {
        private String department;
        private boolean isClosed;
        private String status;
    }
}
