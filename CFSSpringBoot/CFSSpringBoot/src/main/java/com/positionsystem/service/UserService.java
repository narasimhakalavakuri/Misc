
package com.positionsystem.service;

import com.positionsystem.dto.UserDto;
import com.positionsystem.entity.User;
import com.positionsystem.repository.UserRepository;
import com.positionsystem.exception.ResourceNotFoundException;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.time.LocalDateTime;
import java.util.List;
import java.util.stream.Collectors;

@Service
@RequiredArgsConstructor
@Transactional
public class UserService {

    private final UserRepository userRepository;

    public UserDto getUserByUserid(String userid) {
        User user = userRepository.findByUserid(userid)
            .orElseThrow(() -> new ResourceNotFoundException("User not found: " + userid));
        return mapToDto(user);
    }

    public List<UserDto> getAllUsers() {
        return userRepository.findAll().stream()
            .map(this::mapToDto)
            .collect(Collectors.toList());
    }

    public void changePassword(String userid, String oldPassword, String newPassword) {
        User user = userRepository.findByUserid(userid)
            .orElseThrow(() -> new ResourceNotFoundException("User not found"));

        // Verify old password - direct comparison like Delphi
        String encryptedOld = encryptPassword(oldPassword, userid);
        if (!user.getPwd().equals(encryptedOld) && !user.getPwd().equals(oldPassword)) {
            throw new RuntimeException("Old password is incorrect");
        }

        // Set new password (encrypted)
        String encryptedNew = encryptPassword(newPassword, userid);
        user.setPwd(encryptedNew);
        user.setModifiedDate(LocalDateTime.now());
        userRepository.save(user);
    }

    public void deleteUser(Long id) {
        if (!userRepository.existsById(id)) {
            throw new ResourceNotFoundException("User not found with id: " + id);
        }
        userRepository.deleteById(id);
    }

    private UserDto mapToDto(User user) {
        return new UserDto(
            user.getId(),
            user.getUserid(),
            user.getDepartment(),
            user.getAccessMask(),
            user.getFullName(),
            user.getEmail()
        );
    }
    
    /**
     * Matches Delphi encryption logic
     */
    private String encryptPassword(String password, String userid) {
        String key = password + userid.toUpperCase();
        StringBuilder encrypted = new StringBuilder();
        
        for (int i = 0; i < password.length(); i++) {
            char c = password.charAt(i);
            char k = key.charAt(i % key.length());
            encrypted.append((char)(c ^ k));
        }
        
        StringBuilder hex = new StringBuilder();
        for (int i = 0; i < encrypted.length(); i++) {
            hex.append(String.format("%02X", (int)encrypted.charAt(i)));
        }
        
        return hex.toString();
    }
}
