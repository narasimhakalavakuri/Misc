// package com.positionsystem.exception;

// import lombok.AllArgsConstructor;
// import lombok.Data;
// import org.springframework.http.HttpStatus;
// import org.springframework.http.ResponseEntity;
// import org.springframework.web.bind.MethodArgumentNotValidException;
// import org.springframework.web.bind.annotation.ExceptionHandler;
// import org.springframework.web.bind.annotation.RestControllerAdvice;
// import java.time.LocalDateTime;
// import java.util.List;
// import java.util.stream.Collectors;

// @RestControllerAdvice
// public class GlobalExceptionHandler {
    
//     @ExceptionHandler(ValidationException.class)
//     public ResponseEntity<ErrorResponse> handleValidation(ValidationException ex) {
//         ErrorResponse error = new ErrorResponse(
//             HttpStatus.BAD_REQUEST.value(),
//             ex.getMessage(),
//             LocalDateTime.now()
//         );
//         return ResponseEntity.badRequest().body(error);
//     }
    
//     @ExceptionHandler(ResourceNotFoundException.class)
//     public ResponseEntity<ErrorResponse> handleNotFound(ResourceNotFoundException ex) {
//         ErrorResponse error = new ErrorResponse(
//             HttpStatus.NOT_FOUND.value(),
//             ex.getMessage(),
//             LocalDateTime.now()
//         );
//         return ResponseEntity.status(HttpStatus.NOT_FOUND).body(error);
//     }
    
//     @ExceptionHandler(MethodArgumentNotValidException.class)
//     public ResponseEntity<ValidationErrorResponse> handleValidationErrors(
//             MethodArgumentNotValidException ex) {
//         List<String> errors = ex.getBindingResult()
//             .getFieldErrors()
//             .stream()
//             .map(error -> error.getField() + ": " + error.getDefaultMessage())
//             .collect(Collectors.toList());
        
//         ValidationErrorResponse response = new ValidationErrorResponse(
//             HttpStatus.BAD_REQUEST.value(),
//             "Validation failed",
//             errors,
//             LocalDateTime.now()
//         );
//         return ResponseEntity.badRequest().body(response);
//     }
    
//     @ExceptionHandler(Exception.class)
//     public ResponseEntity<ErrorResponse> handleGeneral(Exception ex) {
//         ErrorResponse error = new ErrorResponse(
//             HttpStatus.INTERNAL_SERVER_ERROR.value(),
//             "An unexpected error occurred: " + ex.getMessage(),
//             LocalDateTime.now()
//         );
//         return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR).body(error);
//     }
    
//     @Data
//     @AllArgsConstructor
//     private static class ErrorResponse {
//         private int status;
//         private String message;
//         private LocalDateTime timestamp;
//     }
    
//     @Data
//     @AllArgsConstructor
//     private static class ValidationErrorResponse {
//         private int status;
//         private String message;
//         private List<String> errors;
//         private LocalDateTime timestamp;
//     }
// }


package com.positionsystem.exception;

import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.MethodArgumentNotValidException;
import org.springframework.web.bind.annotation.ExceptionHandler;
import org.springframework.web.bind.annotation.RestControllerAdvice;
import org.springframework.web.context.request.WebRequest;

import java.time.LocalDateTime;
import java.util.HashMap;
import java.util.Map;
import java.util.stream.Collectors;

@RestControllerAdvice
public class GlobalExceptionHandler {

    @ExceptionHandler(ValidationException.class)
    public ResponseEntity<Map<String, Object>> handleValidationException(
            ValidationException ex, WebRequest request) {

        Map<String, Object> response = new HashMap<>();
        response.put("timestamp", LocalDateTime.now());
        response.put("status", HttpStatus.BAD_REQUEST.value());
        response.put("error", "Validation Error");
        response.put("message", ex.getMessage());

        return new ResponseEntity<>(response, HttpStatus.BAD_REQUEST);
    }

    @ExceptionHandler(ResourceNotFoundException.class)
    public ResponseEntity<Map<String, Object>> handleResourceNotFoundException(
            ResourceNotFoundException ex, WebRequest request) {

        Map<String, Object> response = new HashMap<>();
        response.put("timestamp", LocalDateTime.now());
        response.put("status", HttpStatus.NOT_FOUND.value());
        response.put("error", "Not Found");
        response.put("message", ex.getMessage());

        return new ResponseEntity<>(response, HttpStatus.NOT_FOUND);
    }

    @ExceptionHandler(MethodArgumentNotValidException.class)
    public ResponseEntity<Map<String, Object>> handleMethodArgumentNotValid(
            MethodArgumentNotValidException ex, WebRequest request) {

        Map<String, Object> response = new HashMap<>();
        response.put("timestamp", LocalDateTime.now());
        response.put("status", HttpStatus.BAD_REQUEST.value());
        response.put("error", "Validation Error");
        response.put("message", "Invalid input");
        response.put("fields", ex.getBindingResult().getFieldErrors().stream()
            .collect(Collectors.toMap(
                error -> error.getField(),
                error -> error.getDefaultMessage()
            )));

        return new ResponseEntity<>(response, HttpStatus.BAD_REQUEST);
    }

    @ExceptionHandler(Exception.class)
    public ResponseEntity<Map<String, Object>> handleGlobalException(
            Exception ex, WebRequest request) {

        Map<String, Object> response = new HashMap<>();
        response.put("timestamp", LocalDateTime.now());
        response.put("status", HttpStatus.INTERNAL_SERVER_ERROR.value());
        response.put("error", "Internal Server Error");
        response.put("message", ex.getMessage());
        response.put("path", request.getDescription(false).replace("uri=", ""));

        ex.printStackTrace();

        return new ResponseEntity<>(response, HttpStatus.INTERNAL_SERVER_ERROR);
    }
}
