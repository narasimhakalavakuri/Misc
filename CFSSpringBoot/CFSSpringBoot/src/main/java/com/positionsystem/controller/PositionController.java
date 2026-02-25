
package com.positionsystem.controller;

import com.positionsystem.dto.CreatePositionRequest;
import com.positionsystem.dto.PositionEntryDto;
import com.positionsystem.entity.EntryStatus;
import com.positionsystem.entity.PositionEntry;
import com.positionsystem.repository.PositionEntryRepository;
import com.positionsystem.service.PositionEntryService;
import com.positionsystem.exception.ResourceNotFoundException;
import lombok.RequiredArgsConstructor;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.time.LocalDate;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.stream.Collectors;

@RestController
@RequestMapping("/v1/positions")
@RequiredArgsConstructor
public class PositionController {

    private final PositionEntryRepository positionEntryRepository;
    private final PositionEntryService positionEntryService;

    // ===================================================================
    // CREATE & UPDATE ENDPOINTS
    // ===================================================================

    @PostMapping
    public ResponseEntity<Map<String, Object>> createPosition(
            @RequestBody CreatePositionRequest request,
            @RequestHeader(value = "Authorization", required = false) String token) {
        
        try {
            String createdBy = extractUsernameFromToken(token);
            PositionEntryDto created = positionEntryService.createPositionEntry(request, createdBy);
            
            Map<String, Object> response = new HashMap<>();
            response.put("success", true);
            response.put("data", created);
            response.put("message", "Position entry created successfully");
            
            return ResponseEntity.ok(response);
        } catch (Exception e) {
            Map<String, Object> error = new HashMap<>();
            error.put("success", false);
            error.put("error", e.getMessage());
            return ResponseEntity.status(500).body(error);
        }
    }

    @PostMapping("/{uid}/checkout")
    public ResponseEntity<Map<String, Object>> checkoutEntry(
            @PathVariable String uid,
            @RequestHeader(value = "Authorization", required = false) String token) {
        
        try {
            String username = extractUsernameFromToken(token);
            positionEntryService.checkoutEntry(uid, username);
            
            Map<String, Object> response = new HashMap<>();
            response.put("success", true);
            response.put("message", "Entry checked out successfully");
            
            return ResponseEntity.ok(response);
        } catch (Exception e) {
            Map<String, Object> error = new HashMap<>();
            error.put("error", e.getMessage());
            return ResponseEntity.status(500).body(error);
        }
    }

    @PostMapping("/{uid}/checkin")
    public ResponseEntity<Map<String, Object>> checkinEntry(
            @PathVariable String uid,
            @RequestHeader(value = "Authorization", required = false) String token) {
        
        try {
            String username = extractUsernameFromToken(token);
            positionEntryService.checkinEntry(uid, username);
            
            Map<String, Object> response = new HashMap<>();
            response.put("success", true);
            response.put("message", "Entry checked in successfully");
            
            return ResponseEntity.ok(response);
        } catch (Exception e) {
            Map<String, Object> error = new HashMap<>();
            error.put("error", e.getMessage());
            return ResponseEntity.status(500).body(error);
        }
    }

    @PostMapping("/{uid}/approve")
    public ResponseEntity<Map<String, Object>> approveEntry(
            @PathVariable String uid,
            @RequestHeader(value = "Authorization", required = false) String token) {
        
        try {
            String username = extractUsernameFromToken(token);
            positionEntryService.approveEntry(uid, username);
            
            Map<String, Object> response = new HashMap<>();
            response.put("success", true);
            response.put("message", "Entry approved successfully");
            
            return ResponseEntity.ok(response);
        } catch (Exception e) {
            Map<String, Object> error = new HashMap<>();
            error.put("error", e.getMessage());
            return ResponseEntity.status(500).body(error);
        }
    }

    @PostMapping("/{uid}/reject")
    public ResponseEntity<Map<String, Object>> rejectEntry(
            @PathVariable String uid,
            @RequestBody Map<String, String> body,
            @RequestHeader(value = "Authorization", required = false) String token) {
        
        try {
            String username = extractUsernameFromToken(token);
            String reason = body.getOrDefault("reason", "");
            positionEntryService.rejectEntry(uid, reason, username);
            
            Map<String, Object> response = new HashMap<>();
            response.put("success", true);
            response.put("message", "Entry rejected successfully");
            
            return ResponseEntity.ok(response);
        } catch (Exception e) {
            Map<String, Object> error = new HashMap<>();
            error.put("error", e.getMessage());
            return ResponseEntity.status(500).body(error);
        }
    }

    @DeleteMapping("/{uid}")
    public ResponseEntity<Map<String, Object>> deleteEntry(@PathVariable String uid) {
        try {
            positionEntryService.deleteEntry(uid);
            
            Map<String, Object> response = new HashMap<>();
            response.put("success", true);
            response.put("message", "Entry deleted successfully");
            
            return ResponseEntity.ok(response);
        } catch (Exception e) {
            Map<String, Object> error = new HashMap<>();
            error.put("error", e.getMessage());
            return ResponseEntity.status(500).body(error);
        }
    }

    // ===================================================================
    // STATISTICS & SUMMARY ENDPOINTS
    // ===================================================================

    @GetMapping("/statistics")
    public ResponseEntity<Map<String, Object>> getStatistics(
            @RequestParam(defaultValue = "IT") String department) {

        try {
            Map<String, Object> stats = new HashMap<>();
            List<PositionEntry> allEntries = positionEntryRepository.findAll();

            long pendingCount = allEntries.stream()
                .filter(e -> e.getStatus() == EntryStatus.PENDING && e.getDepartment().equals(department))
                .count();
            long approvedCount = allEntries.stream()
                .filter(e -> e.getStatus() == EntryStatus.APPROVED && e.getDepartment().equals(department))
                .count();
            long rejectedCount = allEntries.stream()
                .filter(e -> e.getStatus() == EntryStatus.REJECTED && e.getDepartment().equals(department))
                .count();
            long corrections = allEntries.stream()
                .filter(e -> e.getStatus() == EntryStatus.CORRECTION && e.getDepartment().equals(department))
                .count();

            stats.put("totalEntries", pendingCount + approvedCount + rejectedCount + corrections);
            stats.put("pendingApproval", pendingCount);
            stats.put("approvedToday", approvedCount);
            stats.put("corrections", corrections);
            stats.put("rejected", rejectedCount);

            return ResponseEntity.ok(stats);
        } catch (Exception e) {
            Map<String, Object> error = new HashMap<>();
            error.put("error", e.getMessage());
            return ResponseEntity.status(500).body(error);
        }
    }

    // ===================================================================
    // STATUS-BASED ENDPOINTS
    // ===================================================================

    @GetMapping("/corrections")
    public ResponseEntity<Map<String, Object>> getCorrections(
            @RequestParam(defaultValue = "IT") String department) {

        try {
            List<PositionEntry> corrections = positionEntryRepository.findAll().stream()
                .filter(e -> e.getStatus() == EntryStatus.CORRECTION && e.getDepartment().equals(department))
                .collect(Collectors.toList());

            Map<String, Object> response = new HashMap<>();
            response.put("total", corrections.size());
            response.put("items", corrections.stream().map(this::mapToDto).collect(Collectors.toList()));
            response.put("department", department);

            return ResponseEntity.ok(response);
        } catch (Exception e) {
            Map<String, Object> error = new HashMap<>();
            error.put("error", e.getMessage());
            return ResponseEntity.status(500).body(error);
        }
    }

    @GetMapping("/pending")
    public ResponseEntity<Map<String, Object>> getPending(
            @RequestParam(defaultValue = "IT") String department) {

        try {
            List<PositionEntry> pending = positionEntryRepository.findAll().stream()
                .filter(e -> e.getStatus() == EntryStatus.PENDING && e.getDepartment().equals(department))
                .collect(Collectors.toList());

            Map<String, Object> response = new HashMap<>();
            response.put("total", pending.size());
            response.put("count", pending.size());
            response.put("items", pending.stream().map(this::mapToDto).collect(Collectors.toList()));

            return ResponseEntity.ok(response);
        } catch (Exception e) {
            Map<String, Object> error = new HashMap<>();
            error.put("error", e.getMessage());
            return ResponseEntity.status(500).body(error);
        }
    }

    @GetMapping("/approved")
    public ResponseEntity<Map<String, Object>> getApproved(
            @RequestParam(defaultValue = "IT") String department) {

        try {
            List<PositionEntry> approved = positionEntryRepository.findAll().stream()
                .filter(e -> e.getStatus() == EntryStatus.APPROVED && e.getDepartment().equals(department))
                .collect(Collectors.toList());

            Map<String, Object> response = new HashMap<>();
            response.put("total", approved.size());
            response.put("count", approved.size());
            response.put("items", approved.stream().map(this::mapToDto).collect(Collectors.toList()));

            return ResponseEntity.ok(response);
        } catch (Exception e) {
            Map<String, Object> error = new HashMap<>();
            error.put("error", e.getMessage());
            return ResponseEntity.status(500).body(error);
        }
    }

    @GetMapping("/incomplete")
    public ResponseEntity<Map<String, Object>> getIncomplete(
            @RequestParam(defaultValue = "IT") String department) {

        try {
            List<PositionEntry> incomplete = positionEntryRepository.findAll().stream()
                .filter(e -> e.getStatus() == EntryStatus.INCOMPLETE && e.getDepartment().equals(department))
                .collect(Collectors.toList());

            Map<String, Object> response = new HashMap<>();
            response.put("total", incomplete.size());
            response.put("count", incomplete.size());
            response.put("items", incomplete.stream().map(this::mapToDto).collect(Collectors.toList()));

            return ResponseEntity.ok(response);
        } catch (Exception e) {
            Map<String, Object> error = new HashMap<>();
            error.put("error", e.getMessage());
            return ResponseEntity.status(500).body(error);
        }
    }

    // ===================================================================
    // DATE-BASED FILTERING ENDPOINTS
    // ===================================================================

    @GetMapping("/pending-approvals")
    public ResponseEntity<Map<String, Object>> getPendingApprovals(
            @RequestParam String department,
            @RequestParam String date) {

        try {
            LocalDate valueDate = LocalDate.parse(date);

            List<PositionEntry> entries = positionEntryRepository.findAll().stream()
                .filter(e -> e.getStatus() == EntryStatus.PENDING
                        && e.getDepartment().equals(department)
                        && e.getValueDate().equals(valueDate))
                .collect(Collectors.toList());

            Map<String, Object> response = new HashMap<>();
            response.put("total", entries.size());
            response.put("department", department);
            response.put("date", date);
            response.put("items", entries.stream().map(this::mapToDto).collect(Collectors.toList()));

            return ResponseEntity.ok(response);
        } catch (Exception e) {
            Map<String, Object> error = new HashMap<>();
            error.put("error", "Failed to fetch pending approvals: " + e.getMessage());
            return ResponseEntity.status(500).body(error);
        }
    }

    // ===================================================================
    // GENERAL LIST ENDPOINTS
    // ===================================================================

    @GetMapping
    public ResponseEntity<Map<String, Object>> getAllEntries(
            @RequestParam(defaultValue = "IT") String department,
            @RequestParam(defaultValue = "0") int page,
            @RequestParam(defaultValue = "20") int size) {

        try {
            List<PositionEntry> entries = positionEntryRepository.findAll();
            List<PositionEntry> filtered = entries.stream()
                .filter(e -> e.getDepartment().equals(department))
                .skip((long) page * size)
                .limit(size)
                .collect(Collectors.toList());

            Map<String, Object> response = new HashMap<>();
            response.put("total", entries.size());
            response.put("page", page);
            response.put("size", size);
            response.put("items", filtered.stream().map(this::mapToDto).collect(Collectors.toList()));

            return ResponseEntity.ok(response);
        } catch (Exception e) {
            Map<String, Object> error = new HashMap<>();
            error.put("error", e.getMessage());
            return ResponseEntity.status(500).body(error);
        }
    }

    // ===================================================================
    // DETAIL ENDPOINTS
    // ===================================================================

    @GetMapping("/{uid}")
    public ResponseEntity<Map<String, Object>> getByUid(@PathVariable String uid) {

        try {
            PositionEntry entry = positionEntryRepository.findAll().stream()
                .filter(e -> e.getUid().equals(uid))
                .findFirst()
                .orElseThrow(() -> new ResourceNotFoundException("Position entry not found"));

            Map<String, Object> response = new HashMap<>();
            response.put("success", true);
            response.put("data", mapToDto(entry));

            return ResponseEntity.ok(response);
        } catch (ResourceNotFoundException e) {
            Map<String, Object> error = new HashMap<>();
            error.put("error", e.getMessage());
            return ResponseEntity.status(404).body(error);
        } catch (Exception e) {
            Map<String, Object> error = new HashMap<>();
            error.put("error", e.getMessage());
            return ResponseEntity.status(500).body(error);
        }
    }

    // ===================================================================
    // HELPER METHODS
    // ===================================================================

    private Map<String, Object> mapToDto(PositionEntry entry) {
        Map<String, Object> dto = new HashMap<>();
        dto.put("id", entry.getId());
        dto.put("uid", entry.getUid());
        dto.put("entryDate", entry.getEntryDate());
        dto.put("valueDate", entry.getValueDate());
        dto.put("department", entry.getDepartment());
        dto.put("transactionType", entry.getTransactionType());
        dto.put("reference", entry.getReference());
        dto.put("theirReference", entry.getTheirReference());
        dto.put("inwardCurrency", entry.getInwardCurrency());
        dto.put("inwardAmount", entry.getInwardAmount());
        dto.put("outwardCurrency", entry.getOutwardCurrency());
        dto.put("outwardAmount", entry.getOutwardAmount());
        dto.put("exchangeRate", entry.getExchangeRate());
        dto.put("calcOperator", entry.getCalcOperator());
        dto.put("inwardAccount", entry.getInwardAccount());
        dto.put("inwardAccountName", entry.getInwardAccountName());
        dto.put("outwardAccount", entry.getOutwardAccount());
        dto.put("outwardAccountName", entry.getOutwardAccountName());
        dto.put("status", entry.getStatus().toString());
        dto.put("checkedOutBy", entry.getCheckedOutBy());
        dto.put("approvedDate", entry.getApprovedDate());
        dto.put("createdBy", entry.getCreatedBy());
        dto.put("createdDate", entry.getCreatedDate());
        dto.put("modifiedBy", entry.getModifiedBy());
        dto.put("modifiedDate", entry.getModifiedDate());
        dto.put("isFeExchange", entry.getIsFeExchange());
        return dto;
    }

    private String extractUsernameFromToken(String token) {
        if (token == null || !token.startsWith("jwt-token-")) {
            return "admin"; // Default for testing
        }
        String[] parts = token.split("-");
        return parts.length >= 3 ? parts[2] : "admin";
    }
}
