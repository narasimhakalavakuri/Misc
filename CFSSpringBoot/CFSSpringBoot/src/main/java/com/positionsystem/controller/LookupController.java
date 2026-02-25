
package com.positionsystem.controller;

import com.positionsystem.entity.Currency;
import com.positionsystem.entity.Department;
import com.positionsystem.entity.CustomerAccount;
import com.positionsystem.repository.CurrencyRepository;
import com.positionsystem.repository.DepartmentRepository;
import com.positionsystem.repository.CustomerAccountRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.util.HashMap;
import java.util.Map;
import java.util.stream.Collectors;

@RestController
@RequestMapping("/v1/lookups")
@RequiredArgsConstructor
@CrossOrigin(origins = "*", maxAge = 3600)
public class LookupController {

    private final CurrencyRepository currencyRepository;
    private final DepartmentRepository departmentRepository;
    private final CustomerAccountRepository customerAccountRepository;

    /**
     * Get all currencies
     */
    @GetMapping("/currencies")
    public ResponseEntity<Map<String, Object>> getCurrencies(
            @RequestParam(required = false) String search) {
        try {
            Map<String, Object> response = new HashMap<>();
            
            var currencies = search == null || search.isEmpty()
                ? currencyRepository.findAll()
                : currencyRepository.findByCodeContainingIgnoreCase(search);

            response.put("success", true);
            response.put("total", currencies.size());
            response.put("data", currencies.stream()
                .map(c -> Map.of(
                    "code", (Object) c.getCode(),
                    "name", c.getLongName(),
                    "decimals", c.getDecimals()
                ))
                .collect(Collectors.toList()));

            return ResponseEntity.ok(response);
        } catch (Exception e) {
            Map<String, Object> error = new HashMap<>();
            error.put("error", "Failed to fetch currencies: " + e.getMessage());
            return ResponseEntity.status(500).body(error);
        }
    }

    /**
     * Get all departments
     */
    @GetMapping("/departments")
    public ResponseEntity<Map<String, Object>> getDepartments() {
        try {
            Map<String, Object> response = new HashMap<>();
            
            var departments = departmentRepository.findAll().stream()
                .filter(d -> !Boolean.TRUE.equals(d.getIsClosed()))
                .collect(Collectors.toList());

            response.put("success", true);
            response.put("total", departments.size());
            response.put("data", departments.stream()
                .map(d -> Map.of(
                    "code", (Object) d.getCode(),
                    "description", d.getDescription(),
                    "isClosed", d.getIsClosed()
                ))
                .collect(Collectors.toList()));

            return ResponseEntity.ok(response);
        } catch (Exception e) {
            Map<String, Object> error = new HashMap<>();
            error.put("error", "Failed to fetch departments: " + e.getMessage());
            return ResponseEntity.status(500).body(error);
        }
    }

    /**
     * Get all accounts
     */
    @GetMapping("/accounts")
    public ResponseEntity<Map<String, Object>> getAccounts(
            @RequestParam(required = false) String search) {
        try {
            Map<String, Object> response = new HashMap<>();
            
            var accounts = search == null || search.isEmpty()
                ? customerAccountRepository.findAll()
                : customerAccountRepository.searchAccounts(search);

            response.put("success", true);
            response.put("total", accounts.size());
            response.put("data", accounts.stream()
                .map(a -> Map.of(
                    "accountNo", (Object) a.getAccountNo(),
                    "customerName", a.getCustomerName(),
                    "abbreviation", a.getAbbreviatedName() != null ? a.getAbbreviatedName() : ""
                ))
                .collect(Collectors.toList()));

            return ResponseEntity.ok(response);
        } catch (Exception e) {
            Map<String, Object> error = new HashMap<>();
            error.put("error", "Failed to fetch accounts: " + e.getMessage());
            return ResponseEntity.status(500).body(error);
        }
    }

    /**
     * Get account by account number (called when user enters account code)
     * IMPORTANT: This must match exactly what the frontend sends
     */
    @GetMapping("/accounts/{accountNo}")
    public ResponseEntity<Map<String, Object>> getAccountByNumber(
            @PathVariable String accountNo) {
        try {
            System.out.println("=== LOOKUP ACCOUNT ===");
            System.out.println("Looking up account: " + accountNo);
            System.out.println("Account code type: " + accountNo.getClass().getSimpleName());
            System.out.println("Account code length: " + accountNo.length());
            
            var account = customerAccountRepository.findByAccountNo(accountNo.toUpperCase());
            
            if (account.isPresent()) {
                System.out.println("Account found: " + account.get().getCustomerName());
                Map<String, Object> response = new HashMap<>();
                response.put("success", true);
                response.put("accountNo", account.get().getAccountNo());
                response.put("customerName", account.get().getCustomerName());
                response.put("abbreviatedName", account.get().getAbbreviatedName());
                return ResponseEntity.ok(response);
            } else {
                System.out.println("Account NOT found: " + accountNo);
                Map<String, Object> error = new HashMap<>();
                error.put("success", false);
                error.put("error", "Account not found: " + accountNo);
                return ResponseEntity.status(404).body(error);
            }
        } catch (Exception e) {
            System.err.println("Lookup error: " + e.getMessage());
            e.printStackTrace();
            Map<String, Object> error = new HashMap<>();
            error.put("error", "Failed to fetch account: " + e.getMessage());
            return ResponseEntity.status(500).body(error);
        }
    }
}
